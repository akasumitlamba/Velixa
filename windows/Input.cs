using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace Velixa {
public static class Native {
 public static long GestureInjectionUntil;
 public static readonly UIntPtr InjectionTag=new UIntPtr(0x564C5841u);
 [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr window);
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
 public static void Key(int vk,int scan,bool down,bool extended){var a=new INPUT{type=1,u=new U{key=new KI{vk=(ushort)vk,scan=(ushort)scan,extra=InjectionTag,flags=(uint)((down?0:2)|(extended?1:0))}}};SendInput(1,new[]{a},Marshal.SizeOf(typeof(INPUT)));}
 public static void Position(int x,int y){var r=SystemInformation.VirtualScreen;var a=new INPUT{type=0,u=new U{mouse=new MI{extra=InjectionTag,dx=(int)((x-r.Left)*65535d/Math.Max(1,r.Width-1)),dy=(int)((y-r.Top)*65535d/Math.Max(1,r.Height-1)),flags=0x8000|0x4000|1}}};SendInput(1,new[]{a},Marshal.SizeOf(typeof(INPUT)));}
 public static void Mouse(uint flags,int data=0){var a=new INPUT{type=0,u=new U{mouse=new MI{extra=InjectionTag,flags=flags,data=unchecked((uint)data)}}};SendInput(1,new[]{a},Marshal.SizeOf(typeof(INPUT)));}
}
public class Edge : Form {
 readonly Rectangle target;static readonly List<Edge> flashes=new List<Edge>();
 public static void Clear(){foreach(var edge in flashes.ToArray())edge.Close();flashes.Clear();}
 public Edge(Rectangle bounds,Color color){target=bounds;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;FormBorderStyle=FormBorderStyle.None;MinimumSize=Size.Empty;ShowInTaskbar=false;TopMost=true;BackColor=color;Bounds=bounds;Opacity=.9;}
 protected override void OnShown(EventArgs e){base.OnShown(e);Native.SetWindowPos(Handle,new IntPtr(-1),target.X,target.Y,target.Width,target.Height,0x10|0x40);}
 protected override void SetBoundsCore(int x,int y,int width,int height,BoundsSpecified specified){base.SetBoundsCore(x,y,width,height,specified);}

 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x20|0x80;return p;}}
 public static Rectangle SegmentBounds(Rectangle b,string side,double from,double to){from=Math.Max(0,Math.Min(1,from));to=Math.Max(from,Math.Min(1,to));bool h=side=="left"||side=="right";int start=(int)Math.Round((h?b.Height:b.Width)*from),end=(int)Math.Round((h?b.Height:b.Width)*to);return h?new Rectangle(side=="left"?b.Left:b.Right-6,b.Top+start,6,Math.Max(1,end-start)):new Rectangle(b.Left+start,side=="top"?b.Top:b.Bottom-6,Math.Max(1,end-start),6);}
 public static void Flash(string side,double from=0,double to=1){if(side!="left"&&side!="right"&&side!="top"&&side!="bottom")return;var area=SegmentBounds(SystemInformation.VirtualScreen,side,from,to);foreach(var s in Screen.AllScreens){var r=Rectangle.Intersect(area,s.Bounds);if(r.Width<=0||r.Height<=0)continue;var f=new Edge(r,Color.FromArgb(75,222,242));flashes.Add(f);f.Show();var timer=new System.Windows.Forms.Timer{Interval=40};int ticks=0;f.FormClosed+=(o,e)=>{timer.Stop();timer.Dispose();flashes.Remove(f);};timer.Tick+=(o,e)=>{ticks++;f.Opacity=Math.Max(.05,.95-ticks*.055);if(ticks>=16){timer.Stop();timer.Dispose();f.Close();}};timer.Start();}}

}
public class PointerParking : Form {
 public Func<int,IntPtr,bool> TouchpadMessage;
 protected override void WndProc(ref Message m){if(TouchpadMessage!=null&&TouchpadMessage(m.Msg,m.WParam)){m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}
 IntPtr cursor;
 public PointerParking(Point point){AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;MinimumSize=Size.Empty;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Black;Opacity=.01;Bounds=new Rectangle(point.X-4,point.Y-4,8,8);var andMask=new byte[128];for(int i=0;i<andMask.Length;i++)andMask[i]=255;cursor=Native.CreateCursor(IntPtr.Zero,0,0,32,32,andMask,new byte[128]);Cursor=new Cursor(cursor);}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80;return p;}}
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(cursor!=IntPtr.Zero){Native.DestroyCursor(cursor);cursor=IntPtr.Zero;}}
}
public static class TouchpadBridge {
 public static object Create(string name,params object[] args){try{var path=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Velixa.Touchpad.dll");var type=System.Reflection.Assembly.LoadFrom(path).GetType("Velixa.Touchpad."+name,true);return Activator.CreateInstance(type,args);}catch{return null;}}
 public static bool Call(object target,string method,params object[] args){if(target==null)return false;try{target.GetType().GetMethod(method).Invoke(target,args);return true;}catch{return false;}}
 public static bool Message(object target,int message,IntPtr wparam){if(target==null||(message!=0x245&&message!=0x246&&message!=0x247))return false;try{return (bool)target.GetType().GetMethod("Message").Invoke(target,new object[]{message,wparam});}catch{return false;}}
 public static bool Valid(Dictionary<string,object> packet){
  if(packet.ContainsKey("action")){int action=Wire.I(packet,"action",-1);return action>=0&&action<=5;}
  object raw;if(!packet.TryGetValue("contacts",out raw)||!(raw is System.Collections.IEnumerable))return false;
  int count=0;var ids=new HashSet<int>();foreach(var item in (System.Collections.IEnumerable)raw){var contact=item as Dictionary<string,object>;if(contact==null||++count>5)return false;int id=Wire.I(contact,"id",-1),x=Wire.I(contact,"x",-1),y=Wire.I(contact,"y",-1),flags=Wire.I(contact,"flags",-1);if(id<0||id>65535||!ids.Add(id)||x<0||x>20000||y<0||y>20000||(flags&~0x4006)!=0)return false;}return count>0;
 }
}
public class InputController : IDisposable {
 public List<Peer> Order=new List<Peer>(); // null is the source Windows desktop
 public double LocalX,LocalY,LocalScale=1;public Action PhysicalActivity;public volatile Peer Active;public volatile bool Enabled;public Action Changed;public Action<string> Notify;
 readonly System.Windows.Forms.Timer focusGuard=new System.Windows.Forms.Timer{Interval=100};
 public string GestureState="Gestures available when controlling an updated Windows PC";
 object touchpad;bool touchpadAttempted;IntPtr previousForeground;DateTime gestureUntil=DateTime.MinValue;
 Point lastPhysical;int physicalDistance;DateTime lastPhysicalMove=DateTime.MinValue;
 HookState hook;Rectangle local;Point anchor;PointerParking parking;double x,y;DateTime switched=DateTime.MinValue;volatile bool warp;byte[] keys=new byte[256];HashSet<int> held=new HashSet<int>();HashSet<uint> buttons=new HashSet<uint>();
 public InputController(){Order.Add(null);local=SystemInformation.VirtualScreen;anchor=new Point(Screen.PrimaryScreen.Bounds.Left+Screen.PrimaryScreen.Bounds.Width/2,Screen.PrimaryScreen.Bounds.Top+Screen.PrimaryScreen.Bounds.Height/2);hook=new HookState(this);focusGuard.Tick+=(s,e)=>CheckGestureFocus();}
 public void Add(Peer p){Order.Add(p);if(Changed!=null)Changed();}
 public void Remove(Peer p){if(Active==p)Home();Order.Remove(p);if(Changed!=null)Changed();}
 void StopTouchpad(){focusGuard.Stop();TouchpadBridge.Call(touchpad,"Enable",false);if(parking!=null&&Native.GetForegroundWindow()==parking.Handle&&previousForeground!=IntPtr.Zero)Native.SetForegroundWindow(previousForeground);previousForeground=IntPtr.Zero;}
 void StartTouchpad(){
  if(Active==null||Active.Kind!="Windows"||!Active.Touchpad){StopTouchpad();GestureState="Multi-finger gestures unavailable: update Velixa and Windows on both PCs";if(Notify!=null)Notify(GestureState);return;}
  if(!touchpadAttempted){touchpadAttempted=true;touchpad=TouchpadBridge.Create("Capture",(Action<object>)(packet=>{if(!Enabled||Active==null)return;gestureUntil=DateTime.UtcNow.AddMilliseconds(400);Active.Send(packet);}));}
  if(touchpad==null){if(Notify!=null)Notify("Update Windows to enable multi-finger touchpad sharing");return;}
  TouchpadBridge.Call(touchpad,"Window",parking.Handle);parking.TouchpadMessage=(msg,w)=>TouchpadBridge.Message(touchpad,msg,w);
  if(TouchpadBridge.Call(touchpad,"Enable",true)){if(previousForeground==IntPtr.Zero)previousForeground=Native.GetForegroundWindow();Native.SetForegroundWindow(parking.Handle);if(Native.GetForegroundWindow()!=parking.Handle){Home();GestureState="Remote input stopped: Windows did not grant touchpad capture focus. Try switching again.";if(Notify!=null)Notify(GestureState);return;}GestureState="Gestures routed to "+Active.Name;focusGuard.Start();}
 }
 void CheckGestureFocus(){if(Active==null||!Enabled){focusGuard.Stop();return;}if(parking==null||Native.GetForegroundWindow()!=parking.Handle){Home();GestureState="Control returned here because touchpad capture lost focus";if(Notify!=null)Notify(GestureState);}}
 public void Home(){StopTouchpad();Edge.Clear();if(parking!=null)parking.Hide();if(Active!=null){Active.Send(new{t="leave"});Active=null;Native.Position(anchor.X,anchor.Y);}held.Clear();buttons.Clear();Array.Clear(keys,0,keys.Length);switched=DateTime.UtcNow;if(Changed!=null)Changed();}
 public void Select(Peer p){if(p!=null&&!p.Online)return;if(p==null){Home();return;}SwitchTo(p,.5,"right",false);}
 public RectangleF BoundsOf(Peer p){return DeskGeometry.Bounds(p==null?LocalX:p.X,p==null?LocalY:p.Y,p==null?local.Width:p.Width,p==null?local.Height:p.Height,p==null?LocalScale:p.Scale);}
 static string Opposite(string d){return d=="left"?"right":d=="right"?"left":d=="top"?"bottom":"top";}
 void SwitchTo(Peer next,double ratio,string direction,bool feedback=true){
  if(held.Count>0||buttons.Count>0)return;
  Edge.Clear();
  double from=0,to=1,nextFrom=0,nextTo=1;var previous=BoundsOf(Active);var target=BoundsOf(next);double lo,hi;if(DeskGeometry.Segment(previous,target,direction,out lo,out hi)){from=lo;to=hi;}if(DeskGeometry.Segment(target,previous,Opposite(direction),out lo,out hi)){nextFrom=lo;nextTo=hi;}
  if(Active!=null)Active.Send(new{t="leave",edge=feedback?direction:"",edgeFrom=from,edgeTo=to});else if(feedback)Edge.Flash(direction,from,to);
  Active=next;switched=DateTime.UtcNow;bool horizontal=direction=="left"||direction=="right";
  int width=next==null?local.Width:next.Width,height=next==null?local.Height:next.Height;
  x=horizontal?(direction=="right"?2:width-3):ratio*(width-1);y=horizontal?ratio*(height-1):(direction=="bottom"?2:height-3);
  if(next==null){StopTouchpad();if(parking!=null)parking.Hide();local=SystemInformation.VirtualScreen;Native.Position(local.Left+(int)x,local.Top+(int)y);if(feedback)Edge.Flash(Opposite(direction),nextFrom,nextTo);}
  else{next.Send(new{t="enter",edge=feedback?Opposite(direction):"",x=x/width,y=y/height,edgeFrom=nextFrom,edgeTo=nextTo});if(parking==null)parking=new PointerParking(anchor);parking.Show();Native.SetWindowPos(parking.Handle,new IntPtr(-1),anchor.X-4,anchor.Y-4,8,8,0x10|0x40);Native.Position(anchor.X,anchor.Y);StartTouchpad();}
  if(Changed!=null)Changed();
 }
 public void PreviewEdges(){Edge.Clear();var a=BoundsOf(null);foreach(var p in Order){if(p==null||!p.Online)continue;foreach(string side in new[]{"left","right","top","bottom"}){double from,to;if(DeskGeometry.Segment(a,BoundsOf(p),side,out from,out to))Edge.Flash(side,from,to);}}}
 bool Neighbor(string direction,double ratio){var current=BoundsOf(Active);var rects=new List<RectangleF>();foreach(var p in Order)rects.Add(BoundsOf(p));double nextRatio;int next=DeskGeometry.Find(rects,Order.IndexOf(Active),direction,ratio,i=>Order[i]==null||Order[i].Online,out nextRatio);if(next<0)return false;SwitchTo(Order[next],nextRatio,direction);return true;}
 void MouseInput(int msg,Native.MS m){if(!Enabled){if((msg==0x201||msg==0x204)&&PhysicalActivity!=null)PhysicalActivity();if(msg==0x200){var now=DateTime.UtcNow;var point=new Point(m.pt.x,m.pt.y);if((now-lastPhysicalMove).TotalMilliseconds>250){lastPhysical=point;physicalDistance=0;}physicalDistance+=Math.Min(40,Math.Abs(point.X-lastPhysical.X)+Math.Abs(point.Y-lastPhysical.Y));lastPhysical=point;lastPhysicalMove=now;if(physicalDistance>=12){physicalDistance=0;if(PhysicalActivity!=null)PhysicalActivity();}}return;}
  if(Active==null){if(msg==0x200&&(DateTime.UtcNow-switched).TotalMilliseconds>300&&DateTime.UtcNow>gestureUntil&&held.Count==0&&buttons.Count==0){local=SystemInformation.VirtualScreen;double ratio=(m.pt.y-local.Top)/(double)Math.Max(1,local.Height-1);if(m.pt.x<=local.Left&&Neighbor("left",ratio))return;if(m.pt.x>=local.Right-1&&Neighbor("right",ratio))return;double xr=(m.pt.x-local.Left)/(double)Math.Max(1,local.Width-1);if(m.pt.y<=local.Top&&Neighbor("top",xr))return;if(m.pt.y>=local.Bottom-1&&Neighbor("bottom",xr))return;}TrackButton(msg);return;}
  if(msg==0x200){int dx=m.pt.x-anchor.X,dy=m.pt.y-anchor.Y;if(dx==0&&dy==0)return;x+=dx;y+=dy;if((DateTime.UtcNow-switched).TotalMilliseconds>250&&DateTime.UtcNow>gestureUntil&&held.Count==0&&buttons.Count==0){if(x<0&&Neighbor("left",y/Active.Height))return;if(x>=Active.Width&&Neighbor("right",y/Active.Height))return;if(y<0&&Neighbor("top",x/Active.Width))return;if(y>=Active.Height&&Neighbor("bottom",x/Active.Width))return;}if(Active!=null){x=Math.Max(0,Math.Min(Active.Width-1,x));y=Math.Max(0,Math.Min(Active.Height-1,y));Active.Send(new{t="move",x=x/Active.Width,y=y/Active.Height});}warp=true;Native.Position(anchor.X,anchor.Y);warp=false;
  }else{TrackButton(msg);if(msg==0x201||msg==0x202||msg==0x204||msg==0x205||msg==0x207||msg==0x208)Active.Send(new{t="button",button=(msg==0x201||msg==0x202)?1:(msg==0x204||msg==0x205)?2:3,down=(msg==0x201||msg==0x204||msg==0x207)});if(msg==0x20A||msg==0x20E){switched=DateTime.UtcNow;Active.Send(new{t="wheel",delta=(short)(m.data>>16),horizontal=msg==0x20E});}}
  return;
 }
 void TrackButton(int msg){if(msg==0x201)buttons.Add(1);if(msg==0x202)buttons.Remove(1);if(msg==0x204)buttons.Add(2);if(msg==0x205)buttons.Remove(2);if(msg==0x207)buttons.Add(3);if(msg==0x208)buttons.Remove(3);}
 void KeyInput(int msg,Native.KB k){bool down=msg==0x100||msg==0x104;int vk=(int)k.vk;if(!Enabled){if(down&&PhysicalActivity!=null)PhysicalActivity();return;}if(vk>255)return;
  keys[vk]=(byte)(down?128:0);if(down)held.Add(vk);else held.Remove(vk);
  bool ctrl=held.Contains(162)||held.Contains(163)||held.Contains(17),alt=held.Contains(164)||held.Contains(165)||held.Contains(18);
  if(down&&vk==8&&ctrl&&alt){Home();return;}
  if(Active==null)return;
  keys[16]=(byte)((held.Contains(160)||held.Contains(161))?128:0);keys[17]=(byte)(ctrl?128:0);keys[18]=(byte)(alt?128:0);keys[20]=(byte)(Native.GetKeyState(20)&1);
  string text="";if(down&&!ctrl&&!alt&&!held.Contains(91)&&!held.Contains(92)){var sb=new StringBuilder(8);int count=Native.ToUnicode(k.vk,k.scan,keys,sb,8,4);if(count>0&&sb.Length>0&&!Char.IsControl(sb[0]))text=sb.ToString();}
  Active.Send(new{t="key",vk=vk,scan=k.scan,down=down,ext=(k.flags&1)!=0,text=text,ctrl=ctrl,shift=keys[16]!=0,alt=alt});return;
 }
 public void Dispose(){Home();focusGuard.Dispose();if(touchpad is IDisposable)((IDisposable)touchpad).Dispose();hook.Dispose();if(parking!=null)parking.Dispose();}
 public int HookThreadId{get{return hook.ThreadId;}}
 public int HookEvents{get{return hook.Events;}}
 sealed class HookState:IDisposable {
  readonly InputController owner;readonly Control delivery=new Control();sealed class Work{public Action Run;public bool Motion;}readonly Queue<Work> events=new Queue<Work>();readonly object gate=new object();Work tail;readonly Thread thread;readonly ManualResetEvent ready=new ManualResetEvent(false);Control pump;Native.Hook mouse,key;IntPtr mh,kh;int posted,stopped,count;Exception error;public int ThreadId;public int Events{get{return Volatile.Read(ref count);}}
  public HookState(InputController input){owner=input;delivery.CreateControl();thread=new Thread(Run){IsBackground=true,Name="Velixa input hooks"};thread.SetApartmentState(ApartmentState.STA);thread.Start();if(!ready.WaitOne(5000))throw new InvalidOperationException("Input thread did not start");if(error!=null)throw new InvalidOperationException("Could not capture input",error);}
  void Run(){try{ThreadId=Thread.CurrentThread.ManagedThreadId;pump=new Control();pump.CreateControl();mouse=Mouse;key=Key;mh=Native.SetWindowsHookEx(14,mouse,Native.GetModuleHandle(null),0);kh=Native.SetWindowsHookEx(13,key,Native.GetModuleHandle(null),0);if(mh==IntPtr.Zero||kh==IntPtr.Zero)throw new InvalidOperationException("Input hook unavailable");ready.Set();Application.Run();}catch(Exception e){error=e;ready.Set();}finally{if(mh!=IntPtr.Zero)Native.UnhookWindowsHookEx(mh);if(kh!=IntPtr.Zero)Native.UnhookWindowsHookEx(kh);if(pump!=null)pump.Dispose();}}
  void Post(Action action,bool motion=false){if(Volatile.Read(ref stopped)!=0)return;lock(gate){if(motion&&tail!=null&&tail.Motion){tail.Run=action;}else{if(events.Count>=512){events.Clear();owner.Enabled=false;events.Enqueue(new Work{Run=()=>owner.Home()});}tail=new Work{Run=action,Motion=motion};events.Enqueue(tail);}}Schedule();}
  void Schedule(){if(Interlocked.Exchange(ref posted,1)==0)try{delivery.BeginInvoke((Action)Drain);}catch{Interlocked.Exchange(ref posted,0);}}
  void Drain(){Interlocked.Exchange(ref posted,0);if(Volatile.Read(ref stopped)!=0)return;int batch=0;while(batch++<64){Work work;lock(gate){if(events.Count==0){tail=null;return;}work=events.Dequeue();if(events.Count==0)tail=null;}try{work.Run();}catch{owner.Enabled=false;owner.Home();}}lock(gate){if(events.Count>0)Schedule();}}
  IntPtr Mouse(int n,IntPtr w,IntPtr l){Interlocked.Increment(ref count);if(n<0)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);var data=(Native.MS)Marshal.PtrToStructure(l,typeof(Native.MS));if(data.extra==Native.InjectionTag||owner.warp||((data.flags&1)!=0&&DateTime.UtcNow.Ticks<Interlocked.Read(ref Native.GestureInjectionUntil)))return Native.CallNextHookEx(IntPtr.Zero,n,w,l);int msg=w.ToInt32();bool capture=owner.Enabled&&owner.Active!=null;Post(()=>owner.MouseInput(msg,data),msg==0x200);return capture?(IntPtr)1:Native.CallNextHookEx(IntPtr.Zero,n,w,l);}
  IntPtr Key(int n,IntPtr w,IntPtr l){Interlocked.Increment(ref count);if(n<0)return Native.CallNextHookEx(IntPtr.Zero,n,w,l);var data=(Native.KB)Marshal.PtrToStructure(l,typeof(Native.KB));if(data.extra==Native.InjectionTag||((data.flags&16)!=0&&DateTime.UtcNow.Ticks<Interlocked.Read(ref Native.GestureInjectionUntil)))return Native.CallNextHookEx(IntPtr.Zero,n,w,l);int msg=w.ToInt32();bool capture=owner.Enabled&&owner.Active!=null;Post(()=>owner.KeyInput(msg,data));return capture?(IntPtr)1:Native.CallNextHookEx(IntPtr.Zero,n,w,l);}
  public void Dispose(){if(Interlocked.Exchange(ref stopped,1)!=0)return;delivery.Dispose();if(pump!=null&&!pump.IsDisposed)try{pump.BeginInvoke((Action)(()=>Application.ExitThread()));}catch{}thread.Join(1000);ready.Dispose();}
 }

}
public class Receiver : IDisposable {
 object touchpad;bool touchpadAttempted;
 sealed class Packet{public Dictionary<string,object> Message;public Func<bool> Allowed;}readonly BlockingCollection<Packet> input=new BlockingCollection<Packet>(2048);readonly Thread worker;readonly Control feedback=new Control();volatile bool disposed;
 readonly Action<uint,int> mouseOutput;
 public Receiver(Action<uint,int> output=null){mouseOutput=output??Native.Mouse;feedback.CreateControl();worker=new Thread(()=>{try{foreach(var m in input.GetConsumingEnumerable())if(m.Allowed==null||m.Allowed())Process(m.Message);}catch{}finally{Release();if(touchpad is IDisposable)((IDisposable)touchpad).Dispose();}}){IsBackground=true,Name="Velixa remote input"};worker.Start();}
 public void Handle(Dictionary<string,object> message,Func<bool> allowed=null){if(disposed)return;try{if(!input.TryAdd(new Packet{Message=message,Allowed=allowed})){Packet stale;while(input.TryTake(out stale)){}input.TryAdd(new Packet{Message=Wire.Parse("{\"t\":\"leave\"}")});}}catch(InvalidOperationException){}}
 void Flash(string side,double from=0,double to=1){if(disposed)return;try{feedback.BeginInvoke((Action)(()=>{if(!disposed){Edge.Clear();Edge.Flash(side,from,to);}}));}catch{}}
 void Release(){TouchpadBridge.Call(touchpad,"Reset");active=false;foreach(int k in keys)Native.Key(k,0,false,false);keys.Clear();foreach(int b in buttons)mouseOutput(b==1?4u:b==2?16u:64u,0);buttons.Clear();}
 public void Dispose(){if(disposed)return;disposed=true;input.CompleteAdding();worker.Join(1000);feedback.Dispose();}
 bool active;HashSet<int> keys=new HashSet<int>();HashSet<int> buttons=new HashSet<int>();
 void Process(Dictionary<string,object> m){string t=Wire.S(m,"t");if(t=="enter"){Release();active=true;Flash(Wire.S(m,"edge",""),Number(m,"edgeFrom",0),Number(m,"edgeTo",1));Move(m);return;}if(t=="leave"){if(active)Flash(Wire.S(m,"edge",""),Number(m,"edgeFrom",0),Number(m,"edgeTo",1));Release();return;}if(!active)return;
  if(t=="touchpad"&&TouchpadBridge.Valid(m)){if(!touchpadAttempted){touchpadAttempted=true;touchpad=TouchpadBridge.Create("Injector");}Interlocked.Exchange(ref Native.GestureInjectionUntil,DateTime.UtcNow.AddMilliseconds(500).Ticks);if(!TouchpadBridge.Call(touchpad,"Inject",m))TouchpadBridge.Call(touchpad,"Reset");return;}
  if(t=="move")Move(m);
  if(t=="key"){int vk=Wire.I(m,"vk"),scan=Wire.I(m,"scan");bool down=Wire.S(m,"down")=="True";if(vk<1||vk>254)return;if(down)keys.Add(vk);else keys.Remove(vk);Native.Key(vk,scan,down,Wire.S(m,"ext")=="True");}
  if(t=="button"){int b=Wire.I(m,"button");bool down=Wire.S(m,"down")=="True";if(down)buttons.Add(b);else buttons.Remove(b);mouseOutput(b==1?(down?2u:4u):b==2?(down?8u:16u):(down?32u:64u),0);}
  if(t=="wheel")mouseOutput(Wire.S(m,"horizontal")=="True"?0x1000u:0x800u,Wire.I(m,"delta"));
 }
 static double Number(Dictionary<string,object> m,string key,double fallback){double v;return Double.TryParse(Wire.S(m,key),out v)&&!Double.IsNaN(v)&&!Double.IsInfinity(v)?Math.Max(0,Math.Min(1,v)):fallback;}
 void Move(Dictionary<string,object> m){var r=SystemInformation.VirtualScreen;double x=Convert.ToDouble(m["x"]),y=Convert.ToDouble(m["y"]);Native.Position(r.Left+(int)(Math.Max(0,Math.Min(1,x))*(r.Width-1)),r.Top+(int)(Math.Max(0,Math.Min(1,y))*(r.Height-1)));}
}
}
