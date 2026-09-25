using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Input;
using Windows.UI.Core;

namespace Velixa.Touchpad {
 // Kept in a separate assembly: older Windows versions can run Velixa without
 // loading WinRT types that their OS does not provide.
 public sealed class Capture : IDisposable {
  readonly Action<object> send;TouchpadGesturesController controller;IntPtr window;bool enabled;uint lastFrame=UInt32.MaxValue;
  public Capture(Action<object> output){send=output;if(!TouchpadGesturesController.IsSupported())throw new NotSupportedException("Windows touchpad forwarding is unavailable");controller=TouchpadGesturesController.CreateForProcess();controller.SupportedGestures=TouchpadGlobalGestureKinds.ThreeFingerManipulations|TouchpadGlobalGestureKinds.FourFingerManipulations|TouchpadGlobalGestureKinds.ThreeFingerActions|TouchpadGlobalGestureKinds.FourFingerActions;controller.PointerPressed+=Pointer;controller.PointerMoved+=Pointer;controller.PointerReleased+=Pointer;controller.GlobalActionPerformed+=Action;}
  public void Window(IntPtr handle){window=handle;}
  public void Enable(bool value){enabled=value;controller.Enabled=value;lastFrame=UInt32.MaxValue;if(window!=IntPtr.Zero&&!Interop.RegisterTouchpadCapableWindow(window,value)&&value)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
  public bool Message(int message,IntPtr wparam){if(!enabled||(message!=0x245&&message!=0x246&&message!=0x247))return false;uint id=(uint)(wparam.ToInt64()&65535);Interop.Info info;if(!Interop.GetPointerInfo(id,out info)||info.Type!=5)return false;Frame(id);Interop.SkipPointerFrameMessages(id);return true;}
  void Action(TouchpadGesturesController sender,TouchpadGlobalActionEventArgs e){int a=(int)e.Action;send(new{t="touchpad",action=(a%3)*3+a/3});}
  void Pointer(TouchpadGesturesController sender,PointerEventArgs e){
   e.Handled=true;Frame(e.CurrentPoint.PointerId);
  }
  void Frame(uint id){uint count=5;var frame=new Interop.Touch[5];
   if(!Interop.GetPointerFrameTouchpadInfo(id,ref count,frame)||count==0||count>5)return;
   if(frame[0].Info.Frame==lastFrame)return;lastFrame=frame[0].Info.Frame;
   send(new{t="touchpad",contacts=frame.Take((int)count).Select(v=>new{id=v.Info.Id,x=v.Info.Physical.X,y=v.Info.Physical.Y,flags=v.Info.Flags&0x4006}).ToArray()});
  }
  public void Dispose(){if(controller==null)return;Enable(false);controller.PointerPressed-=Pointer;controller.PointerMoved-=Pointer;controller.PointerReleased-=Pointer;controller.GlobalActionPerformed-=Action;controller=null;}
 }
 public sealed class Injector : IDisposable {
  IntPtr device;readonly Dictionary<uint,Interop.TypeInfo> active=new Dictionary<uint,Interop.TypeInfo>();readonly HashSet<int> presses=new HashSet<int>();
  public Injector(){var parameters=new Interop.Parameters{Type=5,Count=5,Feedback=3,Width=20000,Height=20000,Options=3};device=Interop.CreateSyntheticPointerDevice2(ref parameters);if(device==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
  public void Inject(Dictionary<string,object> packet){
   object action;if(packet.TryGetValue("action",out action)){int value=Convert.ToInt32(action);if(!Interop.InjectTouchpadAction(device,value))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());if(value%3==1)presses.Add(value);if(value%3==2)presses.Remove(value-1);return;}
   var contacts=(System.Collections.IEnumerable)packet["contacts"];var frame=new List<Interop.TypeInfo>();
   foreach(Dictionary<string,object> contact in contacts){var info=new Interop.TypeInfo{Type=5,Touch=new Interop.Touch{Info=new Interop.Info{Type=5,Id=Convert.ToUInt32(contact["id"]),Flags=Convert.ToUInt32(contact["flags"]),Physical=new Interop.Point{X=Convert.ToInt32(contact["x"]),Y=Convert.ToInt32(contact["y"])}}}};frame.Add(info);}
   // Missing contacts are released; network loss/reset must never leave a gesture held.
   foreach(var previous in active.Values)if(!frame.Any(v=>v.Touch.Info.Id==previous.Touch.Info.Id)){var released=previous;released.Touch.Info.Flags=0x4000;frame.Add(released);}
   if(frame.Count>5){Reset();return;}
   var input=frame.ToArray();if(!Interop.InjectSyntheticPointerInput(device,input,(uint)input.Length)){int error=Marshal.GetLastWin32Error();if(error!=21)throw new System.ComponentModel.Win32Exception(error);System.Threading.Thread.Sleep(1);if(!Interop.InjectSyntheticPointerInput(device,input,(uint)input.Length))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
   active.Clear();foreach(var info in frame)if((info.Touch.Info.Flags&4)!=0)active[info.Touch.Info.Id]=info;
  }
  public void Reset(){if(device==IntPtr.Zero)return;var released=active.Values.ToArray();for(int i=0;i<released.Length;i++)released[i].Touch.Info.Flags=0x4000;if(released.Length>0)Interop.InjectSyntheticPointerInput(device,released,(uint)released.Length);active.Clear();foreach(int press in presses)Interop.InjectTouchpadAction(device,press+1);presses.Clear();}
  public void Dispose(){Reset();if(device!=IntPtr.Zero){Interop.DestroySyntheticPointerDevice(device);device=IntPtr.Zero;}}
 }
 public static class Interop {
  [StructLayout(LayoutKind.Sequential)]public struct Point{public int X,Y;}
  [StructLayout(LayoutKind.Sequential)]public struct Rect{public int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential)]public struct Info{public uint Type,Id,Frame,Flags;public IntPtr Source,Target;public Point Pixel,Physical,RawPixel,RawPhysical;public uint Time,History;public int Data;public uint Keys;public ulong Performance;public uint Button;}
  [StructLayout(LayoutKind.Sequential)]public struct Touch{public Info Info;public uint Flags,Mask;public Rect Contact,RawContact;public uint Orientation,Pressure;}
  [StructLayout(LayoutKind.Sequential)]public struct TypeInfo{public uint Type;public Touch Touch;}
  [StructLayout(LayoutKind.Sequential)]public struct Parameters{public uint Type,Count,Feedback;public IntPtr Monitor;public uint Width,Height,Options;}
  [DllImport("user32.dll",SetLastError=true)]public static extern bool RegisterTouchpadCapableWindow(IntPtr window,bool enabled);
  [DllImport("user32.dll")]public static extern bool GetPointerInfo(uint id,out Info info);
  [DllImport("user32.dll")]public static extern bool SkipPointerFrameMessages(uint id);
  [DllImport("user32.dll",SetLastError=true)]public static extern bool GetPointerFrameTouchpadInfo(uint id,ref uint count,[Out]Touch[] frame);
  [DllImport("user32.dll",SetLastError=true)]public static extern IntPtr CreateSyntheticPointerDevice2(ref Parameters parameters);
  [DllImport("user32.dll",SetLastError=true)]public static extern bool InjectSyntheticPointerInput(IntPtr device,TypeInfo[] frame,uint count);
  [DllImport("user32.dll",SetLastError=true)]public static extern bool InjectTouchpadAction(IntPtr device,int action);
  [DllImport("user32.dll")]public static extern void DestroySyntheticPointerDevice(IntPtr device);
 }
}
