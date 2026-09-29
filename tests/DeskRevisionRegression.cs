using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using Velixa;
class DeskRevisionRegression {
 static int count;
 static void Check(bool good,string name){if(!good)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Main(){
  double ratio;
  var row=new List<RectangleF>{new RectangleF(0,0,220,120),new RectangleF(220,0,220,120),new RectangleF(440,0,220,120)};
  Check(DeskGeometry.Find(row,0,"right",.5,i=>i!=1,out ratio)==2,"input crosses a temporarily disconnected screen gap");
  Check(DeskGeometry.Find(row,2,"left",.5,i=>i!=1,out ratio)==0,"input crosses the gap in reverse");
  var column=new List<RectangleF>{new RectangleF(0,0,220,120),new RectangleF(0,120,220,120),new RectangleF(0,240,220,120)};
  Check(DeskGeometry.Find(column,0,"bottom",.5,i=>i!=1,out ratio)==2,"input crosses vertical disconnected gap");
  using(var n=new Network())using(var r=new Receiver())using(var desk=new DeskSession(n,r,a=>a())){
   n.IsHost=true;desk.Host();desk.Input.Enabled=false;
   var peer=new Peer{Id="revision-peer",Name="Dell",Kind="Windows",Sharing=true,NewlyPaired=true,SendAction=o=>{}};
   n.Peers.Add(peer);n.Joined(peer);peer.NewlyPaired=false;desk.Input.Enabled=false;string first=desk.Store.active;
   var saved=desk.Devices.Find(d=>d.id==peer.Id);double x=saved.x,y=saved.y;
   desk.Profile("create","Office");string second=desk.Store.active;
   Check(desk.Devices.Count==1,"new desk does not inherit another desk's membership");
   n.Joined(peer);Check(desk.Devices.Count==1,"background reconnect does not import a different desk's devices");
   desk.AddSavedDevice(peer.Id);Check(desk.Devices.Any(d=>d.id==peer.Id&&d.Available),"saved online device can join another desk without pairing");
   desk.SleepDevice(peer.Id,true);desk.Profile("switch",first);
   saved=desk.Devices.Find(d=>d.id==peer.Id);Check(!saved.sleeping&&saved.x==x&&saved.y==y,"desk switch restores its positions and manual state");
   desk.RemoveDevice(peer.Id);Check(desk.Store.desks.Find(p=>p.id==second).devices.Any(d=>d.id==peer.Id),"removal affects only the selected desk");
   n.Joined(peer);Check(desk.Devices.All(d=>d.id!=peer.Id),"removed device stays removed on background resume");
   desk.Profile("switch",second);Check(desk.Devices.Find(d=>d.id==peer.Id).sleeping,"manual disconnect persists through desk changes");
   desk.SleepDevice(peer.Id,false);var local=desk.Devices.Find(d=>d.id==Network.LocalId);var remote=desk.Devices.Find(d=>d.id==peer.Id);local.media=remote.media=true;local.speakerSource=peer.Id;remote.speakerSource=peer.Id;
   Check(desk.SpeakerTarget(Network.LocalId)==peer.Id,"speaker route resolves to selected PC");
   remote.speakerSource=Network.LocalId;Check(desk.SpeakerTarget(Network.LocalId)=="","speaker routing rejects feedback cycles");
   remote.speakerSource=peer.Id;remote.online=false;Check(desk.SpeakerTarget(Network.LocalId)=="","offline speaker destination is not used");
   remote.online=true;int ready=0;desk.Shared=(from,body)=>{if(Wire.S(body,"kind")=="speaker-ready")ready++;};
   var acknowledgement=Wire.Parse(Wire.Json(new{t="share",to=Network.LocalId,body=new{kind="speaker-ready",id=Guid.NewGuid().ToString("N")}}));
   n.Packet(peer,acknowledgement);Check(ready==1,"coordinator relays destination readiness back to its source");
   local.speakerSource=Network.LocalId;n.Packet(peer,acknowledgement);Check(ready==1,"coordinator rejects acknowledgements outside the selected route");
   desk.Input.Enabled=false;
  }
  Console.WriteLine(count+" desk revision checks passed.");
 }
}
