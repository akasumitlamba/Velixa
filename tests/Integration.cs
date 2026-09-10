using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Drawing;
using Velixa;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls.Crypto;
using Big=Org.BouncyCastle.Math.BigInteger;
class Integration {
 static int checks;static void Check(bool pass,string name){if(!pass)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 sealed class Client:IDisposable {
  public TcpClient Tcp;public SslStream Ssl;public StreamReader R;public StreamWriter W;public string Id=Guid.NewGuid().ToString(),Token,Fp,Basis;public Dictionary<string,object> Hello;
  public Client(){Tcp=new TcpClient("127.0.0.1",Wire.Port);Tcp.ReceiveTimeout=2000;Tcp.SendTimeout=2000;Ssl=new SslStream(Tcp.GetStream(),false,(a,b,c,d)=>true);Ssl.AuthenticateAsClient("Velixa",null,SslProtocols.Tls12,false);R=new StreamReader(Ssl);W=new StreamWriter(Ssl,new UTF8Encoding(false)){AutoFlush=true};Hello=Read();Fp=Wire.Hash(Ssl.RemoteCertificate);}
  public void Send(object o){W.WriteLine(Wire.Json(o));}public Dictionary<string,object> Read(){return Wire.Parse(Wire.ReadLine(R));}
  public void Auth(string mode,string secret,string kind="Windows",bool wrong=false){string cn=Wire.RandomHex(32);Basis="Velixa-v2|"+Wire.S(Hello,"id")+"|"+Id+"|"+Fp+"|"+Wire.S(Hello,"nonce")+"|"+cn;Send(new{mode=mode,id=Id,name="Test device",kind=kind,cn=cn,w=1920,h=1080,proof=mode=="srp"?"":Wire.Mac(secret,Basis+"|client")});if(mode=="srp"){var m=Read();var g=Org.BouncyCastle.Tls.Crypto.Srp6StandardGroups.rfc5054_2048;var srp=new Srp6Client();srp.Init(g.N,g.G,new Sha256Digest(),new SecureRandom());var a=srp.GenerateClientCredentials(Convert.FromBase64String(Wire.S(m,"salt")),Encoding.UTF8.GetBytes(Basis),Encoding.UTF8.GetBytes(secret));srp.CalculateSecret(new Big(Wire.S(m,"b"),16));Send(new{a=a.ToString(16),m=srp.CalculateClientEvidenceMessage().ToString(16)});var evidence=Read();if(!srp.VerifyServerEvidenceMessage(new Big(Wire.S(evidence,"m"),16)))throw new Exception("Server proof");Send(new{proof=Wire.Mac(srp.CalculateSessionKey().ToString(16),Basis+"|confirm")});}var ready=Read();Token=mode=="resume"?secret:Wire.S(ready,"token");if(Wire.S(ready,"t")!="ready"||!Wire.Equal(Wire.S(ready,"proof"),Wire.Mac(Token,Basis+"|server")))throw new Exception("Ready proof");}
  public void Dispose(){Tcp.Close();}
 }
 static void Reject(Action a,string name){bool rejected=false;try{a();}catch{rejected=true;}Check(rejected,name);}
 [STAThread]static void Main(){try{Run();Console.WriteLine(checks+" checks passed");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){using(var network=new Network()){network.Host();network.OpenPairing();string code=network.Code;Check(code.Length==4&&code.All(Char.IsDigit),"four digit code");Check((network.PairUntil-DateTime.UtcNow).TotalSeconds<=120,"two minute lifetime");
 using(var bad=new Client())Reject(()=>bad.Auth("srp",code=="0000"?"0001":"0000"),"wrong PIN rejected");
 string token,id;
 using(var good=new Client()){good.Auth("srp",code);token=good.Token;id=good.Id;Check(token.Length==64,"SRP creates strong per-device token");Check(network.PairUntil<DateTime.UtcNow,"successful pairing closes window");}
 using(var replay=new Client())Reject(()=>replay.Auth("srp",code),"closed PIN cannot pair another device");
 using(var resume=new Client()){resume.Id=id;resume.Auth("resume",token);Check(resume.Token==token,"trusted device reconnects with token");}
 using(var other=new Client())Reject(()=>other.Auth("resume",token),"token is bound to device identity");
 network.OpenPairing();string qr=network.QrToken;using(var android=new Client()){android.Auth("qr",qr,"Android");Check(android.Token.Length==64,"Android QR pairing");}
 using(var reuse=new Client())Reject(()=>reuse.Auth("qr",qr,"Android"),"QR token is single use");
 network.OpenPairing();network.PairUntil=DateTime.UtcNow.AddSeconds(-1);using(var expired=new Client())Reject(()=>expired.Auth("qr",network.QrToken,"Android"),"expired QR rejected");
 network.OpenPairing();code=network.Code;for(int i=0;i<5;i++)using(var c=new Client())try{c.Auth("srp",code=="0000"?"0001":"0000");}catch{}
 using(var exhausted=new Client())Reject(()=>exhausted.Auth("srp",code),"five attempts exhaust PIN window");
 network.OpenPairing();using(var c=new Client()){c.Auth("qr",network.QrToken,"Android");Thread.Sleep(6000);Check(!network.Snapshot().Any(p=>p.Id==c.Id),"offline peers expire after missed heartbeats");}
 }
 Check(!DeskSession.ValidEvent(Wire.Parse("{\"t\":\"move\",\"x\":\"NaN\",\"y\":0}")),"nonfinite pointer rejected");Check(!DeskSession.ValidEvent(Wire.Parse("{\"t\":\"enter\",\"x\":2,\"y\":0}")),"out of range pointer rejected");
 var rects=new List<RectangleF>{new RectangleF(0,0,220,124),new RectangleF(220,0,100,220),new RectangleF(0,-130,220,130),new RectangleF(0,124,220,124)};double ratio;
 Check(DeskGeometry.Find(rects,0,"right",.5,i=>true,out ratio)==1&&Math.Abs(ratio-62d/220)<.001,"right crossing preserves vertical position");Check(DeskGeometry.Find(rects,0,"top",.3,i=>true,out ratio)==2,"top crossing");Check(DeskGeometry.Find(rects,0,"bottom",.6,i=>true,out ratio)==3,"bottom crossing");Check(DeskGeometry.Find(rects,1,"left",.9,i=>i==0,out ratio)==-1,"nonoverlapping edge cannot switch");Check(DeskGeometry.Find(rects,0,"right",.5,i=>i!=1,out ratio)==-1,"offline screens are skipped");
 using(var network=new Network()){network.IsHost=true;using(var desk=new DeskSession(network,new Receiver(),a=>a())){desk.Host();desk.Input.Enabled=false;var events=new List<Dictionary<string,object>>();var first=new Peer{Id=Guid.NewGuid().ToString(),Name="Laptop",Kind="Windows",SendAction=o=>events.Add(Wire.Parse(Wire.Json(o)))};var target=new Peer{Id=Guid.NewGuid().ToString(),Name="Phone",Kind="Android",SendAction=o=>events.Add(Wire.Parse(Wire.Json(o)))};network.Peers.Add(first);network.Peers.Add(target);network.Joined(first);network.Joined(target);desk.SelectSource(first.Id);long epoch=desk.Epoch;events.Clear();network.Packet(first,Wire.Parse(Wire.Json(new{t="route",id=target.Id,epoch=epoch,@event=new{t="move",x=.5,y=.5}})));Check(events.Count==1&&Wire.S(events[0],"t")=="move","selected Windows source routes to Android");events.Clear();network.Packet(target,Wire.Parse(Wire.Json(new{t="route",id=first.Id,epoch=epoch,@event=new{t="move",x=.5,y=.5}})));Check(events.Count==0,"Android cannot send input");desk.SelectSource(Network.LocalId);desk.Input.Enabled=false;events.Clear();network.Packet(first,Wire.Parse(Wire.Json(new{t="route",id=target.Id,epoch=epoch,@event=new{t="key",vk=65,down=true}})));Check(events.Count==0,"revoked source and stale epoch rejected");desk.Position(target.Id,120,-200);Check(desk.Devices.Find(d=>d.id==target.Id).y==-200,"two dimensional layout saved");network.Peers.Remove(first);network.Left(first);Check(desk.Devices.Any(d=>d.id==first.Id&&!d.online),"offline device remains on desk");desk.Input.Enabled=false;}}
 }
}


