using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Velixa;
class RecoveryRegression {
 static volatile bool stop;
 static TcpClient active;
 static string hostId, token;
 static System.Security.Cryptography.X509Certificates.X509Certificate2 cert;
 static void Need(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static void Serve(TcpClient socket) {
  try {
   using(socket) {
    socket.ReceiveTimeout=5000; socket.SendTimeout=5000;
    using(var tls=new SslStream(socket.GetStream(),false)) {
     tls.AuthenticateAsServer(cert,false,SslProtocols.Tls12,false);
     var reader=new StreamReader(tls);
     var writer=new StreamWriter(tls,new UTF8Encoding(false)){AutoFlush=true};
     string nonce=Wire.RandomHex(32);
     writer.WriteLine(Wire.Json(new{t="hello",v=2,id=hostId,nonce=nonce}));
     var message=Wire.Parse(Wire.ReadLine(reader));
     string basis="Velixa-v2|"+hostId+"|"+Wire.S(message,"id")+"|"+Wire.Hash(cert)+"|"+nonce+"|"+Wire.S(message,"cn");
     if(!Wire.Equal(Wire.S(message,"proof"),Wire.Mac(token,basis+"|client")))throw new Exception("Bad client proof");
     writer.WriteLine(Wire.Json(new{t="ready",proof=Wire.Mac(token,basis+"|server")}));
     writer.WriteLine(Wire.Json(new{t="desk"}));
     active=socket;
     while(!stop){writer.WriteLine(Wire.Json(new{t="ping"}));Wire.ReadLine(reader);Wire.ReadLine(reader);Thread.Sleep(500);}
    }
   }
  } catch { }
 }
 static void Main() {
  cert=Wire.Certificate();hostId=Guid.NewGuid().ToString();token=Wire.RandomHex(32);
  Wire.SaveSecret("remote-token-v2.bin",token);Wire.SaveSecret("remote-fp-v2.bin",Wire.Hash(cert));Wire.SaveSecret("remote-id-v2.bin",hostId);
  int attempts=0,ready=0;
  using(var client=new Network()) {
   client.Status=s=>{if(s.StartsWith("Connecting to saved desk"))Interlocked.Increment(ref attempts);};
   client.Received=m=>{if(Wire.S(m,"t")=="desk")Interlocked.Increment(ref ready);};
   client.Connect("127.0.0.1","",1920,1080);
   var until=DateTime.UtcNow.AddSeconds(55);
   while(attempts<4&&DateTime.UtcNow<until)Thread.Sleep(30);
   Need(attempts>=4&&client.Connecting,"saved pairing survives more than three failures");
   var server=new TcpListener(IPAddress.Loopback,Wire.Port);server.Start();
   Task.Run(()=>{while(!stop)try{var socket=server.AcceptTcpClient();Task.Run(()=>Serve(socket));}catch{break;}});
   try {
    until=DateTime.UtcNow.AddSeconds(25);while(ready<1&&DateTime.UtcNow<until)Thread.Sleep(30);
    Need(ready>=1&&!client.Connecting,"host becoming available reconnects without Retry or pairing");
    Thread.Sleep(6500);Need(ready==1&&!client.Connecting,"idle authenticated connection stays established with heartbeats");
    active.Close();until=DateTime.UtcNow.AddSeconds(15);while(ready<2&&DateTime.UtcNow<until)Thread.Sleep(30);
    Need(ready>=2,"broken established socket reconnects with the saved identity");
   } finally {stop=true;server.Stop();if(active!=null)active.Close();client.Dispose();}
   Thread.Sleep(1000);Need(!client.Connecting,"shutdown cancels recovery");
  }
  Console.WriteLine("Recovery regression passed.");
 }
}
