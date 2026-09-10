using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace Velixa {
public static class DeskGeometry {
 public static RectangleF Bounds(double x,double y,int width,int height){float factor=220f/Math.Max(width,height);return new RectangleF((float)x,(float)y,width*factor,height*factor);}
 public static int Find(List<RectangleF> r,int current,string direction,double ratio,Func<int,bool> online,out double nextRatio){nextRatio=ratio;if(current<0)return -1;var a=r[current];bool h=direction=="left"||direction=="right";float cross=(h?a.Top:a.Left)+(float)Math.Max(0,Math.Min(1,ratio))*(h?a.Height:a.Width);int best=-1;float distance=Single.MaxValue;
  for(int i=0;i<r.Count;i++){if(i==current||!online(i))continue;var b=r[i];float gap=direction=="right"?b.Left-a.Right:direction=="left"?a.Left-b.Right:direction=="bottom"?b.Top-a.Bottom:a.Top-b.Bottom;float low=h?b.Top:b.Left,high=h?b.Bottom:b.Right;if(gap< -1||gap>120||cross<low||cross>high||gap>=distance)continue;distance=gap;best=i;nextRatio=(cross-low)/(high-low);}return best;
 }
}
public class Device {
 public string id,name,kind;public int w=1920,h=1080;public double x,y;public bool online;
 [ScriptIgnore] public RectangleF Bounds{get{return DeskGeometry.Bounds(x,y,w,h);}}
}
public class DeskSession : IDisposable {
 public List<Device> Devices=new List<Device>();public string Source="";public long Epoch;public InputController Input=new InputController();public Action Changed;public bool Follow;public Action AskSource;
 readonly Network network;readonly Receiver receiver;readonly Action<Action> ui;DateTime lastActivity=DateTime.MinValue;public bool Connected,Preview;
 public DeskSession(Network n,Receiver r,Action<Action> dispatch){network=n;receiver=r;ui=dispatch;Input.PhysicalActivity=()=>{if(!Connected||Source==Network.LocalId||(DateTime.UtcNow-lastActivity).TotalSeconds<20)return;lastActivity=DateTime.UtcNow;if(Follow)SelectSource(Network.LocalId);else if(AskSource!=null)AskSource();};Input.Changed=()=>{if(Changed!=null)Changed();};}
 public void Host(){try{Devices=new JavaScriptSerializer().Deserialize<List<Device>>(Wire.LoadSecret("desk-v2.bin","[]"));}catch{}foreach(var d in Devices)d.online=false;var local=Devices.Find(d=>d.id==Network.LocalId);if(local==null){local=new Device{id=Network.LocalId,name=Environment.MachineName,kind="Windows"};Devices.Insert(0,local);}local.online=true;local.name=Environment.MachineName;var bounds=SystemInformation.VirtualScreen;local.w=bounds.Width;local.h=bounds.Height;Source=local.id;Connected=true;Epoch=1;
  network.Joined=p=>ui(()=>{var d=Devices.Find(v=>v.id==p.Id);if(d==null){d=new Device{id=p.Id,x=Devices.Count==0?0:Devices.Max(v=>v.Bounds.Right)+24};Devices.Add(d);}d.name=p.Name;d.kind=p.Kind;d.w=p.Width;d.h=p.Height;d.online=true;Publish();});
  network.Left=p=>ui(()=>{if(network.Snapshot().Any(v=>v.Id==p.Id))return;var d=Devices.Find(v=>v.id==p.Id);if(d!=null)d.online=false;Reset();if(Source==p.Id)Source=Network.LocalId;Publish();});network.Packet=(p,m)=>ui(()=>HandlePeer(p,m));Publish();
 }
 object State(){return new{t="desk",source=Source,epoch=Epoch,devices=Devices};}
 void Publish(){if(!Preview)Wire.SaveSecret("desk-v2.bin",Wire.Json(Devices));Apply();foreach(var p in network.Snapshot())p.Send(State());}
 void Apply(){Input.Home();Input.Order.Clear();foreach(var d in Devices){if(d.id==Network.LocalId){Input.LocalX=d.x;Input.LocalY=d.y;Input.Order.Add(null);}else{string id=d.id;Input.Order.Add(new Peer{Id=id,Name=d.name,Kind=d.kind,Width=d.w,Height=d.h,X=d.x,Y=d.y,Online=d.online,SendAction=o=>Route(id,o)});}}if(!Devices.Any(d=>d.id==Network.LocalId))Input.Order.Insert(0,null);Input.Enabled=!Preview&&Connected&&Source==Network.LocalId;if(Changed!=null)Changed();}
 void Reset(){Input.Home();receiver.Handle(Wire.Parse("{\"t\":\"leave\"}"));foreach(var p in network.Snapshot())p.Send(new{t="leave"});Epoch++;}
 public void SelectSource(string id){if(!Connected)return;if(!network.IsHost){network.Send(new{t="source",id=id});return;}var d=Devices.Find(v=>v.id==id);if(d==null||!d.online||d.kind!="Windows"||Source==id)return;Reset();Source=id;Publish();}
 public void Position(string id,double x,double y){if(!Connected||Double.IsNaN(x)||Double.IsNaN(y)||Double.IsInfinity(x)||Double.IsInfinity(y))return;x=Math.Max(-5000,Math.Min(5000,x));y=Math.Max(-5000,Math.Min(5000,y));if(!network.IsHost){network.Send(new{t="layout",id=id,x=x,y=y});return;}var d=Devices.Find(v=>v.id==id);if(d==null)return;d.x=x;d.y=y;Reset();Publish();}
 void HandlePeer(Peer p,Dictionary<string,object> m){string t=Wire.S(m,"t");if(t=="source"&&p.Kind=="Windows")SelectSource(Wire.S(m,"id"));if(t=="layout"){double x,y;if(Double.TryParse(Wire.S(m,"x"),out x)&&Double.TryParse(Wire.S(m,"y"),out y))Position(Wire.S(m,"id"),x,y);}if(t=="route"&&p.Kind=="Windows"&&p.Id==Source&&Wire.S(m,"epoch")==Epoch.ToString()){object e;if(m.TryGetValue("event",out e)&&e is Dictionary<string,object>)Deliver(Wire.S(m,"id"),(Dictionary<string,object>)e);}if(t=="size"){var d=Devices.Find(v=>v.id==p.Id);if(d!=null&&(d.w!=p.Width||d.h!=p.Height)){d.w=p.Width;d.h=p.Height;Reset();Publish();}}}
 void Route(string id,object message){if(!Connected||Source!=Network.LocalId)return;var m=Wire.Parse(Wire.Json(message));if(network.IsHost)Deliver(id,m);else network.Send(new{t="route",id=id,epoch=Epoch,@event=m});}
 void Deliver(string id,Dictionary<string,object> m){if(!ValidEvent(m))return;string t=Wire.S(m,"t");if(t!="enter"&&t!="leave"&&t!="move"&&t!="key"&&t!="button"&&t!="wheel")return;if(id==Network.LocalId)receiver.Handle(m);else{var p=network.Snapshot().FirstOrDefault(v=>v.Id==id);if(p!=null)p.Send(m);}}
 public static bool ValidEvent(Dictionary<string,object> m){string t=Wire.S(m,"t");if(t=="enter"||t=="move"){double x,y;if(!Double.TryParse(Wire.S(m,"x"),out x)||!Double.TryParse(Wire.S(m,"y"),out y)||Double.IsNaN(x)||Double.IsNaN(y)||x<0||x>1||y<0||y>1)return false;}if(t=="enter"||t=="leave"){string edge=Wire.S(m,"edge","left");return edge=="left"||edge=="right"||edge=="top"||edge=="bottom";}if(t=="key")return Wire.I(m,"vk")>0&&Wire.I(m,"vk")<255&&Wire.I(m,"scan")>=0&&Wire.I(m,"scan")<256&&Wire.S(m,"text").Length<=32&&(Wire.S(m,"down")=="True"||Wire.S(m,"down")=="False");if(t=="button")return Wire.I(m,"button")>=1&&Wire.I(m,"button")<=3&&(Wire.S(m,"down")=="True"||Wire.S(m,"down")=="False");if(t=="wheel")return Math.Abs((long)Wire.I(m,"delta"))<=12000;return t=="move";}
 public void Receive(Dictionary<string,object> m){string t=Wire.S(m,"t");if(t=="desk"){Connected=true;Source=Wire.S(m,"source");Epoch=Convert.ToInt64(m["epoch"]);Devices=new JavaScriptSerializer().Deserialize<List<Device>>(Wire.Json(m["devices"]));Apply();}else if(t=="offline"){Connected=false;Input.Home();Input.Enabled=false;foreach(var d in Devices)d.online=d.id==Network.LocalId;receiver.Handle(Wire.Parse("{\"t\":\"leave\"}"));if(Changed!=null)Changed();}else receiver.Handle(m);}
 public void Dispose(){Connected=false;Input.Dispose();}
}
}
