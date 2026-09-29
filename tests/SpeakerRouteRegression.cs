using System;
using System.Collections.Generic;
using System.Linq;
using Velixa;
class SpeakerRouteRegression {
 static int checks;static void Check(bool good,string message){if(!good)throw new Exception(message);Console.WriteLine("PASS "+message);checks++;}
 static void Main(){try{using(var network=new Network())using(var receiver=new Receiver())using(var desk=new DeskSession(network,receiver,a=>a())){
  network.IsHost=true;desk.Host();desk.Input.Enabled=false;var local=desk.Devices.First(d=>d.id==Network.LocalId);local.media=true;local.speakerSource="speaker-test";var remote=new Device{id="speaker-test",name="Output PC",kind="Windows",online=true,awake=true,sharing=true,media=true,speakerSource="speaker-test"};desk.Devices.Add(remote);
  var packets=new List<Dictionary<string,object>>();using(var speaker=new SpeakerShare((to,body)=>{lock(packets)packets.Add(Wire.Parse(Wire.Json(body)));},a=>{},()=>desk,()=>true,()=>"")){
   speaker.Reconcile();Check(!speaker.Ready,"source keeps local output until destination confirms readiness");string id=packets.Last(m=>Wire.S(m,"kind")=="speaker-start")["id"].ToString();
   speaker.Handle(remote.id,Wire.Parse(Wire.Json(new{kind="speaker-ready",id=Guid.NewGuid().ToString("N")})));Check(!speaker.Ready,"stale speaker acknowledgement cannot mute local output");
   speaker.Handle("wrong-peer",Wire.Parse(Wire.Json(new{kind="speaker-ready",id=id})));Check(!speaker.Ready,"other peers cannot acknowledge a speaker route");
   speaker.Handle(remote.id,Wire.Parse(Wire.Json(new{kind="speaker-ready",id=id})));Check(speaker.Ready,"selected destination acknowledges the current stream");
   typeof(SpeakerShare).GetField("lastReady",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(speaker,DateTime.UtcNow.AddSeconds(-7));Check(!speaker.Ready,"missing destination acknowledgements restore local-output eligibility");
   remote.online=false;speaker.Reconcile();Check(!speaker.Ready,"disconnect stops the outgoing route");
  }
  remote.online=true;local.speakerSource=Network.LocalId;remote.speakerSource=Network.LocalId;packets.Clear();using(var speaker=new SpeakerShare((to,body)=>packets.Add(Wire.Parse(Wire.Json(body))),a=>a(),()=>desk,()=>true,()=>"")){
   string id=Guid.NewGuid().ToString("N");speaker.Handle(remote.id,Wire.Parse(Wire.Json(new{kind="speaker-start",id=id})));Check(packets.Any(m=>Wire.S(m,"kind")=="speaker-ready"&&Wire.S(m,"id")==id),"destination opens physical playback before acknowledging");speaker.Handle(remote.id,Wire.Parse(Wire.Json(new{kind="speaker-stop",id=id})));speaker.Stop();
  }
  desk.Input.Enabled=false;
 }Console.WriteLine(checks+" speaker route checks passed");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
}
