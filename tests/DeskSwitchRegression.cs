using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using Velixa;
class DeskSwitchRegression {
 sealed class Host:IDisposable {
  public string Id=Guid.NewGuid().ToString(),Token=Wire.RandomHex(32),Address,Name;public int Active;TcpListener server;volatile bool stopped;
  readonly System.Security.Cryptography.X509Certificates.X509Certificate2 certificate=Wire.Certificate();
  public Host(string address,string name){Address=address;Name=name;server=new TcpListener(IPAddress.Parse(address),Wire.Port);server.Start();Task.Run(()=>{while(!stopped)try{var socket=server.AcceptTcpClient();Task.Run(()=>Serve(socket));}catch{break;}});}
  public void Save(){Wire.SaveSecret("remote-id-v2.bin",Id);Wire.SaveSecret("remote-token-v2.bin",Token);Wire.SaveSecret("remote-fp-v2.bin",Wire.Hash(certificate));Wire.SaveSecret("host.bin",Address);Wire.SaveSecret("client-devices.bin",Wire.Json(new[]{new Device{id=Id,name=Name,kind="Windows"}}));Wire.SaveSecret("pending-removals.bin","[]");SavedDesks.Remember();}
  void Serve(TcpClient socket){bool accepted=false;try{using(socket)using(var tls=new SslStream(socket.GetStream(),false)){socket.ReceiveTimeout=15000;tls.AuthenticateAsServer(certificate,false,SslProtocols.Tls12,false);var reader=new StreamReader(tls);var writer=new StreamWriter(tls,new UTF8Encoding(false)){AutoFlush=true};string nonce=Wire.RandomHex(32);writer.WriteLine(Wire.Json(new{t="hello",v=2,id=Id,nonce=nonce}));var m=Wire.Parse(Wire.ReadLine(reader));string basis="Velixa-v2|"+Id+"|"+Wire.S(m,"id")+"|"+Wire.Hash(certificate)+"|"+nonce+"|"+Wire.S(m,"cn");if(Wire.S(m,"mode")!="resume"||!Wire.Equal(Wire.S(m,"proof"),Wire.Mac(Token,basis+"|client")))throw new Exception("Wrong desk credentials");writer.WriteLine(Wire.Json(new{t="ready",proof=Wire.Mac(Token,basis+"|server")}));Interlocked.Increment(ref Active);accepted=true;writer.WriteLine(Wire.Json(new{t="desk",desk=Id,epoch=1,source=Id,automatic=false,desks=new[]{new DeskProfile{id=Id,name=Name}},devices=new[]{new Device{id=Id,name=Name,kind="Windows",online=true,awake=true,sharing=true},new Device{id=Network.LocalId,name="Test PC",kind="Windows",online=true,awake=true,sharing=true}}}));while(!stopped)Wire.ReadLine(reader,65536);}}catch{}finally{if(accepted)Interlocked.Decrement(ref Active);}}
  public void Dispose(){stopped=true;server.Stop();}
 }
 static int checks;
 static object Get(object o,string key){return o.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
 static void Call(object o,string key,params object[] args){o.GetType().GetMethod(key,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static void Wait(Func<bool> ready,string text){DateTime end=DateTime.UtcNow.AddSeconds(12);while(!ready()&&DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(10);}Check(ready(),text);}
 [STAThread]static void Main(){try{Application.EnableVisualStyles();using(var form=new MainForm(new[]{"--preview-desk"}){Opacity=0,ShowInTaskbar=false}){form.Show();Application.DoEvents();
  using(var first=new Host("127.0.0.1","Office"))using(var second=new Host("127.0.0.2","Home")){
   first.Save();second.Save();Check(SavedDesks.All().Count==2,"two coordinators retain separate pairing credentials");SavedDesks.ClearActive();
   Call(form,"SwitchSavedDesk",first.Id);Wait(()=>first.Active==1&&((DeskSession)Get(form,"session")).Connected,"first saved desk connects without pairing again");
   Call(form,"SwitchSavedDesk",second.Id);Wait(()=>first.Active==0&&second.Active==1&&((DeskSession)Get(form,"session")).Connected,"switch closes old connection before new desk is ready");
   Check(Wire.LoadSecret("remote-id-v2.bin","")==second.Id&&((DeskSession)Get(form,"session")).Devices.Any(d=>d.id==second.Id),"credentials and device layout belong to the selected desk");
   Call(form,"SwitchSavedDesk",first.Id);Wait(()=>second.Active==0&&first.Active==1&&((DeskSession)Get(form,"session")).Connected,"switch back reuses the first desk's saved identity");
   Call(form,"LeaveCurrentDesk");Wait(()=>first.Active==0,"leaving closes the active desk connection");Check(Get(form,"session")==null&&Wire.LoadSecret("role.bin","")==""&&SavedDesks.All().Count==2,"leave disables startup reconnect and keeps both saved desks");
  }
  Call(form,"SwitchLocalDesk","");var session=(DeskSession)Get(form,"session");Check(((Network)Get(form,"network")).IsHost&&session.Connected,"a former receiver can start its own desk");session.Profile("create","Local office");string office=session.Store.active;session.Profile("create","Local home");Call(form,"SwitchLocalDesk",office);session=(DeskSession)Get(form,"session");Check(session.Store.active==office&&session.DeskName=="Local office","local desk switch restores the chosen saved profile");Call(form,"LeaveCurrentDesk");
  form.GetType().GetField("closing",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(form,true);form.Close();
 }Console.WriteLine(checks+" desk switching checks passed");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
}
