using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Velixa {
public static class Native {
 public delegate IntPtr Hook(int n,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern IntPtr SetWindowsHookEx(int id,Hook proc,IntPtr module,uint thread);
 [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr h,int n,IntPtr w,IntPtr l);
 [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string s);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int k);
 [DllImport("user32.dll")] public static extern short GetKeyState(int k);
 [DllImport("user32.dll")] public static extern int ToUnicode(uint vk,uint scan,byte[] state,StringBuilder chars,int count,uint flags);
 [DllImport("user32.dll")] public static extern uint SendInput(uint count,INPUT[] input,int size);
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT r);
 [StructLayout(LayoutKind.Sequential)] public struct RECT {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] public static extern IntPtr CreateCursor(IntPtr instance,int hotX,int hotY,int width,int height,byte[] andMask,byte[] xorMask);
 [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr cursor);
 [StructLayout(LayoutKind.Sequential)] public struct POINT {public int x,y;}
 [StructLayout(LayoutKind.Sequential)] public struct MS {public POINT pt;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)] public struct KB {public uint vk,scan,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)] public struct MI {public int dx,dy;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)] public struct KI {public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Explicit)] public struct U {[FieldOffset(0)]public MI mouse;[FieldOffset(0)]public KI key;}
 [StructLayout(LayoutKind.Sequential)] public struct INPUT {public uint type;public U u;}
 public static void Key(int vk,int scan,bool down,bool extended){var a=new INPUT{type=1,u=new U{key=new KI{vk=(ushort)vk,scan=(ushort)scan,flags=(uint)((down?0:2)|(extended?1:0))}}};SendInput(1,new[]{a},Marshal.SizeOf(typeof(INPUT)));}
 public static void Mouse(uint flags,int data=0){var a=new INPUT{type=0,u=new U{mouse=new MI{flags=flags,data=unchecked((uint)data)}}};SendInput(1,new[]{a},Marshal.SizeOf(typeof(INPUT)));}
}
public class Edge : Form {
 readonly Rectangle target;
 public Edge(Rectangle bounds,Color color){target=bounds;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;FormBorderStyle=FormBorderStyle.None;MinimumSize=Size.Empty;ShowInTaskbar=false;TopMost=true;BackColor=color;Bounds=bounds;Opacity=.9;}
 protected override void OnShown(EventArgs e){base.OnShown(e);Native.SetWindowPos(Handle,new IntPtr(-1),target.X,target.Y,target.Width,target.Height,0x10|0x40);}
 protected override void SetBoundsCore(int x,int y,int width,int height,BoundsSpecified specified){base.SetBoundsCore(x,y,width,height,specified);}

 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x20|0x80;return p;}}
 public static void Flash(string side){foreach(var s in Screen.AllScreens){var b=s.Bounds;var r=side=="left"?new Rectangle(b.Left,b.Top,6,b.Height):side=="right"?new Rectangle(b.Right-6,b.Top,6,b.Height):side=="top"?new Rectangle(b.Left,b.Top,b.Width,6):new Rectangle(b.Left,b.Bottom-6,b.Width,6);var f=new Edge(r,Color.FromArgb(75,222,242));f.Show();var timer=new Timer{Interval=40};int ticks=0;timer.Tick+=(o,e)=>{ticks++;f.Opacity=Math.Max(.05,.95-ticks*.055);if(ticks>=16){timer.Stop();timer.Dispose();f.Close();}};timer.Start();}}
}
public class PointerParking : Form {
 IntPtr cursor;
 public PointerParking(Point point){AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;MinimumSize=Size.Empty;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Black;Opacity=.01;Bounds=new Rectangle(point.X-4,point.Y-4,8,8);var andMask=new byte[128];for(int i=0;i<andMask.Length;i++)andMask[i]=255;cursor=Native.CreateCursor(IntPtr.Zero,0,0,32,32,andMask,new byte[128]);Cursor=new Cursor(cursor);}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(cursor!=IntPtr.Zero){Native.DestroyCursor(cursor);cursor=IntPtr.Zero;}}
}
public class InputController : IDisposable {
 public List<Peer> Order=new List<Peer>(); // null is the source Windows desktop
 public double LocalX,LocalY;public Action PhysicalActivity;public Peer Active;public bool Enabled;public Action Changed;public Action<string> Notify;
 HookState hook;Rectangle local;Point anchor;PointerParking parking;double x,y;DateTime switched=DateTime.MinValue;bool warp;byte[] keys=new byte[256];HashSet<int> held=new HashSet<int>();HashSet<uint> buttons=new HashSet<uint>();
 public InputController(){Order.Add(null);local=SystemInformation.VirtualScreen;anchor=new Point(Screen.PrimaryScreen.Bounds.Left+Screen.PrimaryScreen.Bounds.Width/2,Screen.PrimaryScreen.Bounds.Top+Screen.PrimaryScreen.Bounds.Height/2);hook=new HookState(MouseHook,KeyHook);}
 public void Add(Peer p){Order.Add(p);if(Changed!=null)Changed();}
 public void Remove(Peer p){if(Active==p)Home();Order.Remove(p);if(Changed!=null)Changed();}
 public void Home(){if(parking!=null)parking.Hide();if(Active!=null){Active.Send(new{t="leave"});Active=null;Native.SetCursorPos(anchor.X,anchor.Y);Edge.Flash("left");}held.Clear();buttons.Clear();Array.Clear(keys,0,keys.Length);switched=DateTime.UtcNow;if(Changed!=null)Changed();}
 public void Select(Peer p){if(p!=null&&!p.Online)return;if(p==null){Home();return;}SwitchTo(p,.5,"right");}
 public RectangleF BoundsOf(Peer p){return DeskGeometry.Bounds(p==null?LocalX:p.X,p==null?LocalY:p.Y,p==null?local.Width:p.Width,p==null?local.Height:p.Height);}
 static string Opposite(string d){return d=="left"?"right":d=="right"?"left":d=="top"?"bottom":"top";}
 void SwitchTo(Peer next,double ratio,string direction){
  if(held.Count>0||buttons.Count>0)return;
  if(Active!=null)Active.Send(new{t="leave",edge=direction});else Edge.Flash(direction);
  Active=next;switched=DateTime.UtcNow;bool horizontal=direction=="left"||direction=="right";
  int width=next==null?local.Width:next.Width,height=next==null?local.Height:next.Height;
  x=horizontal?(direction=="right"?2:width-3):ratio*(width-1);y=horizontal?ratio*(height-1):(direction=="bottom"?2:height-3);
  if(next==null){if(parking!=null)parking.Hide();local=SystemInformation.VirtualScreen;Native.SetCursorPos(local.Left+(int)x,local.Top+(int)y);Edge.Flash(Opposite(direction));}
  else{next.Send(new{t="enter",edge=Opposite(direction),x=x/width,y=y/height});if(parking==null)parking=new PointerParking(anchor);parking.Show();Native.SetWindowPos(parking.Handle,new IntPtr(-1),anchor.X-4,anchor.Y-4,8,8,0x10|0x40);Native.SetCursorPos(anchor.X,anchor.Y);}
  if(Changed!=null)Changed();
 }
 bool Neighbor(string direction,double ratio){var current=BoundsOf(Active);var rects=new List<RectangleF>();foreach(var p in Order)rects.Add(BoundsOf(p));double nextRatio;int next=DeskGeometry.Find(rects,Order.IndexOf(Active),direction,ratio,i=>Order[i]==null||Order[i].Online,out nextRatio);if(next<0)return false;SwitchTo(Order[next],nextRatio,direction);return true;}
 IntPtr MouseHook(int n,IntPtr w,IntPtr l){if(n<0)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);var m=(Native.MS)Marshal.PtrToStructure(l,typeof(Native.MS));if((m.flags&1)!=0||warp)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);int msg=w.ToInt32();if(!Enabled){if((msg==0x201||msg==0x204)&&PhysicalActivity!=null)PhysicalActivity();return Native.CallNextHookEx(IntPtr.Zero,n,w,l);}
  if(Active==null){if(msg==0x200&&(DateTime.UtcNow-switched).TotalMilliseconds>300&&held.Count==0&&buttons.Count==0){local=SystemInformation.VirtualScreen;double ratio=(m.pt.y-local.Top)/(double)Math.Max(1,local.Height-1);if(m.pt.x<=local.Left&&Neighbor("left",ratio))return (IntPtr)1;if(m.pt.x>=local.Right-1&&Neighbor("right",ratio))return (IntPtr)1;double xr=(m.pt.x-local.Left)/(double)Math.Max(1,local.Width-1);if(m.pt.y<=local.Top&&Neighbor("top",xr))return (IntPtr)1;if(m.pt.y>=local.Bottom-1&&Neighbor("bottom",xr))return (IntPtr)1;}TrackButton(msg);return Native.CallNextHookEx(IntPtr.Zero,n,w,l);}
  if(msg==0x200){int dx=m.pt.x-anchor.X,dy=m.pt.y-anchor.Y;if(dx==0&&dy==0)return (IntPtr)1;x+=dx;y+=dy;if((DateTime.UtcNow-switched).TotalMilliseconds>250&&held.Count==0&&buttons.Count==0){if(x<0&&Neighbor("left",y/Active.Height))return (IntPtr)1;if(x>=Active.Width&&Neighbor("right",y/Active.Height))return (IntPtr)1;if(y<0&&Neighbor("top",x/Active.Width))return (IntPtr)1;if(y>=Active.Height&&Neighbor("bottom",x/Active.Width))return (IntPtr)1;}if(Active!=null){x=Math.Max(0,Math.Min(Active.Width-1,x));y=Math.Max(0,Math.Min(Active.Height-1,y));Active.Send(new{t="move",x=x/Active.Width,y=y/Active.Height});}warp=true;Native.SetCursorPos(anchor.X,anchor.Y);warp=false;
  }else{TrackButton(msg);if(msg==0x201||msg==0x202||msg==0x204||msg==0x205||msg==0x207||msg==0x208)Active.Send(new{t="button",button=(msg==0x201||msg==0x202)?1:(msg==0x204||msg==0x205)?2:3,down=(msg==0x201||msg==0x204||msg==0x207)});if(msg==0x20A)Active.Send(new{t="wheel",delta=(short)(m.data>>16)});}
  return (IntPtr)1;
 }
 void TrackButton(int msg){if(msg==0x201)buttons.Add(1);if(msg==0x202)buttons.Remove(1);if(msg==0x204)buttons.Add(2);if(msg==0x205)buttons.Remove(2);if(msg==0x207)buttons.Add(3);if(msg==0x208)buttons.Remove(3);}
 IntPtr KeyHook(int n,IntPtr w,IntPtr l){if(n<0)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);var k=(Native.KB)Marshal.PtrToStructure(l,typeof(Native.KB));if((k.flags&16)!=0)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);bool down=w.ToInt32()==0x100||w.ToInt32()==0x104;int vk=(int)k.vk;if(!Enabled){if(down&&PhysicalActivity!=null)PhysicalActivity();return Native.CallNextHookEx(IntPtr.Zero,n,w,l);}if(vk>255)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);
  keys[vk]=(byte)(down?128:0);if(down)held.Add(vk);else held.Remove(vk);
  bool ctrl=held.Contains(162)||held.Contains(163)||held.Contains(17),alt=held.Contains(164)||held.Contains(165)||held.Contains(18);
  if(down&&vk==8&&ctrl&&alt){Home();return (IntPtr)1;}
  if(Active==null)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);
  keys[16]=(byte)((held.Contains(160)||held.Contains(161))?128:0);keys[17]=(byte)(ctrl?128:0);keys[18]=(byte)(alt?128:0);keys[20]=(byte)(Native.GetKeyState(20)&1);
  string text="";if(down&&!ctrl&&!alt&&!held.Contains(91)&&!held.Contains(92)){var sb=new StringBuilder(8);int count=Native.ToUnicode(k.vk,k.scan,keys,sb,8,4);if(count>0&&sb.Length>0&&!Char.IsControl(sb[0]))text=sb.ToString();}
  Active.Send(new{t="key",vk=vk,scan=k.scan,down=down,ext=(k.flags&1)!=0,text=text,ctrl=ctrl,shift=keys[16]!=0,alt=alt});return (IntPtr)1;
 }
 public void Dispose(){Home();hook.Dispose();if(parking!=null)parking.Dispose();}
 class HookState:IDisposable{Native.Hook m,k;IntPtr mh,kh;public HookState(Native.Hook mouse,Native.Hook key){m=mouse;k=key;var mod=Native.GetModuleHandle(null);mh=Native.SetWindowsHookEx(14,m,mod,0);kh=Native.SetWindowsHookEx(13,k,mod,0);if(mh==IntPtr.Zero||kh==IntPtr.Zero)throw new InvalidOperationException("Could not capture keyboard and mouse");}public void Dispose(){Native.UnhookWindowsHookEx(mh);Native.UnhookWindowsHookEx(kh);}}
}
public class Receiver {
 bool active;HashSet<int> keys=new HashSet<int>();HashSet<int> buttons=new HashSet<int>();
 public void Handle(Dictionary<string,object> m){string t=Wire.S(m,"t");if(t=="enter"){active=true;Edge.Flash(Wire.S(m,"edge","left"));Move(m);return;}if(t=="leave"){if(active)Edge.Flash(Wire.S(m,"edge","right"));active=false;foreach(int k in keys)Native.Key(k,0,false,false);keys.Clear();foreach(int b in buttons)Native.Mouse(b==1?4u:b==2?16u:64u);buttons.Clear();return;}if(!active)return;
  if(t=="move")Move(m);
  if(t=="key"){int vk=Wire.I(m,"vk"),scan=Wire.I(m,"scan");bool down=Wire.S(m,"down")=="True";if(vk<1||vk>254)return;if(down)keys.Add(vk);else keys.Remove(vk);Native.Key(vk,scan,down,Wire.S(m,"ext")=="True");}
  if(t=="button"){int b=Wire.I(m,"button");bool down=Wire.S(m,"down")=="True";if(down)buttons.Add(b);else buttons.Remove(b);Native.Mouse(b==1?(down?2u:4u):b==2?(down?8u:16u):(down?32u:64u));}
  if(t=="wheel")Native.Mouse(0x800,Wire.I(m,"delta"));
 }
 void Move(Dictionary<string,object> m){var r=SystemInformation.VirtualScreen;double x=Convert.ToDouble(m["x"]),y=Convert.ToDouble(m["y"]);Native.SetCursorPos(r.Left+(int)(Math.Max(0,Math.Min(1,x))*(r.Width-1)),r.Top+(int)(Math.Max(0,Math.Min(1,y))*(r.Height-1)));}
}
}
