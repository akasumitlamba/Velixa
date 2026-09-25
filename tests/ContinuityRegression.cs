using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Velixa;
public static class ContinuityRegression {
 static int checks;static readonly ConcurrentQueue<Action> pending=new ConcurrentQueue<Action>();
 static Dictionary<string,object> M(object value){return Wire.Parse(Wire.Json(value));}
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static void Pump(Func<bool> complete){var until=DateTime.UtcNow.AddSeconds(4);do{Action action;while(pending.TryDequeue(out action))action();if(complete())return;Thread.Sleep(2);}while(DateTime.UtcNow<until);throw new Exception("Timed out waiting for audio routing");}
 sealed class Audio:IMicrophoneAudio {
  public int Played;public bool Cable=true,Fail;public readonly ConcurrentDictionary<int,int> Captures=new ConcurrentDictionary<int,int>();
  public WaveAudio.Device[] Devices(bool input){return input?new[]{new WaveAudio.Device{Id=0,Name="Built-in microphone"},new WaveAudio.Device{Id=1,Name="USB microphone"}}:Cable?new[]{new WaveAudio.Device{Id=0,Name="Speakers"},new WaveAudio.Device{Id=7,Name="CABLE Input (VB-Audio Virtual Cable)"}}:new[]{new WaveAudio.Device{Id=0,Name="Speakers"}};}
  public void Capture(int device,Action<byte[]> chunk,CancellationToken cancel){Captures.AddOrUpdate(device,1,(k,v)=>v+1);try{while(!cancel.WaitHandle.WaitOne(10))chunk(new byte[3840]);}finally{Captures.AddOrUpdate(device,0,(k,v)=>v-1);}}
  public void Play(int device,BlockingCollection<byte[]> audio,CancellationToken cancel,Action ready){if(Fail)throw new Exception("Output unplugged");ready();while(!cancel.IsCancellationRequested){byte[] bytes;if(audio.TryTake(out bytes,25,cancel))Interlocked.Increment(ref Played);}}
 }
 [STAThread]static void Main(){try{Routing();Input();Offline();Console.WriteLine(checks+" continuity regression checks passed");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
 static void Routing(){
  var nodes=new Dictionary<string,MicrophoneShare>();var backends=new Dictionary<string,Audio>();var sent=new List<Tuple<string,string,Dictionary<string,object>>>();var offline=new HashSet<string>();
  foreach(string name in new[]{"A","B","C"}){string local=name;var audio=new Audio();backends[name]=audio;nodes[name]=new MicrophoneShare((to,o)=>{var m=M(o);sent.Add(Tuple.Create(local,to,m));nodes[to].Handle(local,m);},work=>pending.Enqueue(work),peer=>!offline.Contains(peer),audio){AllowRemoteRequests=true};}
  var a=nodes["A"];var b=nodes["B"];var c=nodes["C"];
  try{
   Check(a.ResolveOutput()==7,"automatic app routing chooses cable instead of speakers");backends["A"].Cable=false;
   Check(!a.RequestRemoteMic("B",1)&&!b.Sending,"missing app endpoint does not start remote recording");backends["A"].Cable=true;
   string unsolicited=Guid.NewGuid().ToString("N");a.ReceiveEnabled=true;a.Handle("B",M(new{kind="mic-begin",id=unsolicited,rate=48000,channels=1,bits=16}));
   Check(!a.Receiving&&!b.Sending,"unsolicited audio rejected even with incoming audio enabled");
   Check(a.RequestRemoteMic("B",1),"receiver selection starts a remote microphone without source-side buttons");Pump(()=>backends["A"].Played>=3);
   Check(a.Receiving&&b.Sending&&backends["B"].Captures.ContainsKey(1),"selected USB source supplies receiving PC");
   Check(!sent.Any(v=>v.Item1=="B"&&v.Item2=="C"&&Wire.S(v.Item3,"kind")=="mic-begin"),"selection is not broadcast to unselected receivers");
   Check(c.RequestRemoteMic("B",0),"second PC chooses a different microphone from the same source");Pump(()=>backends["C"].Played>=3);
   Check(a.Receiving&&c.Receiving&&backends["B"].Captures[0]>0&&backends["B"].Captures[1]>0,"independent simultaneous input selections remain active");
   Check(b.RequestRemoteMic("A",0),"a sending PC can also select its own remote input");Pump(()=>backends["B"].Played>=3);Check(b.Sending&&b.Receiving,"sending and receiving are independent");
   var old=sent.Last(v=>v.Item1=="B"&&v.Item2=="A"&&Wire.S(v.Item3,"kind")=="mic-begin").Item3;
   Check(a.RequestRemoteMic("B",0),"switching source requests new device");Pump(()=>a.Receiving&&!a.Pending);
   a.Handle("B",M(new{kind="mic-stop",id=Wire.S(old,"id")}));Check(a.Receiving,"stale stop cannot stop replacement stream");
   a.Handle("B",M(new{kind="mic-error",id=Wire.S(old,"id"),message="stale error"}));Check(a.Receiving&&a.State!="stale error","stale error cannot overwrite current source");
   a.StopReceiving();Check(!a.Receiving&&c.Receiving&&b.Sending,"stopping one receiver preserves other consumers");
   b.AllowRemoteRequests=false;Check(a.RequestRemoteMic("B",0),"denied source returns a correlated response");Check(!a.Pending&&!a.Receiving&&a.State.Contains("disabled"),"source denial clears pending request and explains failure");b.AllowRemoteRequests=true;
   Check(a.SelectLocal(1),"local input uses the same app microphone endpoint");Pump(()=>a.Receiving);Check(c.Receiving&&a.Sending,"local selection preserves streams requested by other PCs");a.StopReceiving();
   backends["A"].Fail=true;a.RequestRemoteMic("B",0);Pump(()=>!a.Receiving&&!a.Pending&&a.State=="Output unplugged");Check(c.Receiving,"output failure only stops the affected receiver");backends["A"].Fail=false;
   a.RequestRemoteMic("B",0);a.StopReceiving();Pump(()=>!a.Receiving);Check(!sent.Where(v=>v.Item1=="A"&&Wire.S(v.Item3,"kind")=="mic-accept").Any(v=>Wire.S(v.Item3,"ok")=="True"&&Wire.S(v.Item3,"id")==Wire.S(sent.Last(x=>x.Item1=="B"&&x.Item2=="A"&&Wire.S(x.Item3,"kind")=="mic-begin").Item3,"id")),"cancel before playback opens cannot accept delayed audio");
   offline.Add("B");typeof(MicrophoneShare).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);Check(!c.Receiving&&c.State.Contains("disconnected"),"disconnect stops playback and reports it");
   a.OutputName="Unplugged output";Check(a.ResolveOutput()==-1,"unplugged explicit output never silently falls back to speakers");
  }finally{foreach(var node in nodes.Values)node.Dispose();Pump(()=>backends.Values.All(v=>v.Captures.Values.All(n=>n==0)));}
 }
 static void Input(){
  var outgoing=new List<Dictionary<string,object>>();using(var input=new InputController()){input.Enabled=true;input.Active=new Peer{Kind="Windows",SendAction=o=>outgoing.Add(M(o))};var method=typeof(InputController).GetMethod("MouseInput",BindingFlags.NonPublic|BindingFlags.Instance);
   method.Invoke(input,new object[]{0x20A,new Native.MS{data=unchecked((uint)(-30<<16))}});method.Invoke(input,new object[]{0x20E,new Native.MS{data=60u<<16}});
   Check(Wire.I(outgoing[0],"delta")==-30,"high resolution vertical scrolling preserves signed partial delta");Check(Wire.S(outgoing[1],"horizontal")=="True"&&Wire.I(outgoing[1],"delta")==60,"horizontal scrolling is forwarded separately");
   input.Home();Check(!outgoing.Last().ContainsKey("edge"),"home/reset does not invent an edge");
  }
  uint flags=0;int delta=0;using(var receiver=new Receiver((f,d)=>{flags=f;delta=d;})){typeof(Receiver).GetField("active",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(receiver,true);var method=typeof(Receiver).GetMethod("Process",BindingFlags.NonPublic|BindingFlags.Instance);method.Invoke(receiver,new object[]{M(new{t="wheel",delta=-30,horizontal=true})});Check(flags==0x1000&&delta==-30,"receiver injects horizontal wheel with exact delta");method.Invoke(receiver,new object[]{M(new{t="wheel",delta=15})});Check(flags==0x800&&delta==15,"legacy vertical wheel packets remain compatible");}
  Check(DeskSession.ValidEvent(M(new{t="leave"}))&&DeskSession.ValidEvent(M(new{t="enter",edge="",x=.5,y=.5})),"silent reset and manual selection pass routing validation");
  Check(!DeskSession.ValidEvent(M(new{t="enter",edge="diagonal",x=.5,y=.5})),"unknown edges rejected");
  var contacts=new[]{new{id=1,x=3000,y=4000,flags=0x4006},new{id=2,x=4500,y=4000,flags=0x4006},new{id=3,x=6000,y=4000,flags=0x4006}};
  Check(DeskSession.ValidEvent(M(new{t="touchpad",contacts=contacts})),"three-finger contacts pass encrypted input validation");
  Check(!DeskSession.ValidEvent(M(new{t="touchpad",contacts=Enumerable.Repeat(contacts[0],6).ToArray()})),"oversized and duplicate contact frames rejected");
  Check(!DeskSession.ValidEvent(M(new{t="touchpad",action=99})),"unknown gesture action rejected");
  Check(DeskSession.ValidEvent(M(new{t="touchpad",action=5})),"four-finger release supported");
 }
 static void Offline(){
  var devices=new[]{new Device{id=Network.LocalId,name="Here",kind="Windows",online=true},new Device{id="remote-offline",name="Away",kind="Windows",online=true}};
  Wire.SaveSecret("client-devices.bin",Wire.Json(devices));Wire.SaveSecret("pending-removals.bin","[]");
  using(var n=new Network())using(var r=new Receiver())using(var d=new DeskSession(n,r,a=>a())){d.RestoreClient();Check(!d.Devices[1].online&&!d.Input.Enabled,"saved peers start offline without enabling input");d.RemoveDevice("remote-offline");Check(d.Devices.Count==1,"offline peer can be removed immediately");}
  using(var n=new Network())using(var r=new Receiver())using(var d=new DeskSession(n,r,a=>a())){d.RestoreClient();Check(d.Devices.Count==1,"offline removal survives app restart");d.Paused=true;d.Receive(M(new{t="desk",source=Network.LocalId,epoch=1,devices=devices}));Check(d.Devices.Count==1,"stale coordinator snapshot cannot resurrect removed peer");Check(!d.Input.Enabled,"desk updates cannot resume paused input");d.Receive(M(new{t="desk",source=Network.LocalId,epoch=2,devices=devices.Take(1).ToArray()}));Check(Wire.LoadSecret("pending-removals.bin","")=="[]","coordinator acknowledgement clears pending removal");}
  Wire.SaveSecret("remote-token-v2.bin",new string('a',64));Wire.SaveSecret("remote-id-v2.bin","offline-test");
  using(var n=new Network()){int attempts=0;string status="";n.Status=value=>{status=value;if(value.StartsWith("Connecting to desk"))Interlocked.Increment(ref attempts);};n.Connect("127.0.0.1","",1920,1080);var until=DateTime.UtcNow.AddSeconds(35);while(n.Connecting&&DateTime.UtcNow<until)Thread.Sleep(50);Check(!n.Connecting&&attempts==Network.MaximumConnectAttempts,"offline reconnect stops after three attempts");Check(status.Contains("Retry"),"offline status offers explicit retry");Thread.Sleep(3500);Check(attempts==3,"offline coordinator does not restart reconnect loop");}
 }

}
