using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Velixa.Touchpad;
public static class TouchpadRegression {
 sealed class Surface:Form{public Capture GestureCapture;protected override void WndProc(ref Message m){if(GestureCapture!=null&&GestureCapture.Message(m.Msg,m.WParam)){m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}}
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 static readonly List<Dictionary<string,object>> packets=new List<Dictionary<string,object>>();static int checks;
 static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static void Pump(int milliseconds){var end=DateTime.UtcNow.AddMilliseconds(milliseconds);while(DateTime.UtcNow<end){Application.DoEvents();System.Threading.Thread.Sleep(2);}}
 [STAThread]public static void Main(){IntPtr previous=GetForegroundWindow();Point pointer=Cursor.Position;try{Application.EnableVisualStyles();Run();Console.WriteLine(checks+" native touchpad checks passed (synthetic input, no physical trackpad)");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}finally{SetForegroundWindow(previous);Cursor.Position=pointer;}}
 static void Run(){
  Check(Marshal.SizeOf(typeof(Interop.Info))==96&&Marshal.SizeOf(typeof(Interop.Touch))==144&&Marshal.SizeOf(typeof(Interop.TypeInfo))==152,"x64 touchpad ABI matches Windows");
  using(var capture=new Capture(packet=>{var serializer=new JavaScriptSerializer();packets.Add(serializer.Deserialize<Dictionary<string,object>>(serializer.Serialize(packet)));}))
  using(var form=new Surface{GestureCapture=capture,Text="Velixa gesture verification",Size=new Size(180,90),ShowInTaskbar=false}){
   form.Show();Check(SetForegroundWindow(form.Handle),"test process receives foreground gestures");capture.Window(form.Handle);capture.Enable(true);Cursor.Position=form.PointToScreen(new Point(70,50));Pump(100);
   var parameters=new Interop.Parameters{Type=5,Count=5,Feedback=3,Width=10000,Height=6000,Options=3};IntPtr device=Interop.CreateSyntheticPointerDevice2(ref parameters);
   Check(device!=IntPtr.Zero,"Windows creates synthetic precision touchpad");
   try{foreach(int fingers in new[]{2,3,4}){
    packets.Clear();var frame=Enumerable.Range(0,fingers).Select(i=>new Interop.TypeInfo{Type=5,Touch=new Interop.Touch{Info=new Interop.Info{Type=5,Id=(uint)i,Flags=0x4006,Physical=new Interop.Point{X=2500+i*1400,Y=3000}}}}).ToArray();
    for(int step=0;step<6;step++){for(int i=0;i<fingers;i++){if(step>0&&step<5)frame[i].Touch.Info.Physical.X+=250;if(step==5)frame[i].Touch.Info.Flags=0x4000;}Check(Interop.InjectSyntheticPointerInput(device,frame,(uint)fingers),fingers+" fingers: native input frame "+step);Pump(80);}
    Check(packets.Count>=3,fingers+" finger gesture captured as multiple physical frames");
    var contacts=(System.Collections.IEnumerable)packets.Last()["contacts"];int released=0;foreach(Dictionary<string,object> contact in contacts){if((Convert.ToInt32(contact["flags"])&4)==0)released++;Check(Convert.ToInt32(contact["x"])>=0&&Convert.ToInt32(contact["x"])<=20000,"captured physical coordinates in protocol range");}
    Check(released==fingers,"release frame preserves every lifted finger");
    // Replay the wire representation through the production injector. Capture
    // keeps the synthetic test gesture from acting on the foreground desktop.
    var replay=packets.ToArray();using(var injector=new Injector()){foreach(var packet in replay){injector.Inject(packet);Pump(15);}injector.Reset();}
    Check(true,fingers+" finger wire frames accepted by production injector");Pump(100);
   }}finally{Interop.DestroySyntheticPointerDevice(device);capture.Enable(false);form.Close();}
  }
 }
}
