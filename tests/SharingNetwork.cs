using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Velixa;
public static class SharingNetwork {
 sealed class Client:IDisposable {
  public readonly string Id=Guid.NewGuid().ToString();readonly TcpClient tcp=new TcpClient();readonly StreamReader reader;readonly StreamWriter writer;readonly object sync=new object();public ShareService Shares;public string Received;public long Frames;
  public Client(Network host,string folder){tcp.NoDelay=true;tcp.Connect("127.0.0.1",Wire.Port);tcp.ReceiveTimeout=15000;var ssl=new SslStream(tcp.GetStream(),false,(s,c,ch,e)=>Wire.Hash(c)==host.Fingerprint);ssl.AuthenticateAsClient("Velixa",null,SslProtocols.Tls12,false);reader=new StreamReader(ssl);writer=new StreamWriter(ssl,new UTF8Encoding(false)){AutoFlush=true};var hello=Wire.Parse(Wire.ReadLine(reader));host.OpenPairing();string cn=Wire.RandomHex(32),basis="Velixa-v2|"+Wire.S(hello,"id")+"|"+Id+"|"+host.Fingerprint+"|"+Wire.S(hello,"nonce")+"|"+cn;Write(new{mode="qr",id=Id,name="Sharing test",kind="Windows",sharing=1,cn=cn,w=1920,h=1080,proof=Wire.Mac(host.QrToken,basis+"|client")});var ready=Wire.Parse(Wire.ReadLine(reader));if(Wire.S(ready,"t")!="ready"||!Wire.Equal(Wire.S(ready,"proof"),Wire.Mac(Wire.S(ready,"token"),basis+"|server")))throw new Exception("Authentication failed");Shares=new ShareService((to,body)=>Write(new{t="share",to=to,body=body}),a=>a(),()=>new string[0],folder,false);Shares.ReceivedFile=(from,path)=>Received=path;Task.Run(()=>{try{while(true){var message=Wire.Parse(Wire.ReadLine(reader));string t=Wire.S(message,"t");if(t=="ping")Write(new{t="pong"});if(t=="share"){Interlocked.Increment(ref Frames);Shares.Handle(Wire.S(message,"from"),(System.Collections.Generic.Dictionary<string,object>)message["body"]);}}}catch{}});}
  void Write(object data){lock(sync)writer.WriteLine(Wire.Json(data));}
  public void Dispose(){tcp.Close();Shares.Dispose();}
 }
 static void Pump(ConcurrentQueue<Action> queue){Action action;while(queue.TryDequeue(out action))action();System.Windows.Forms.Application.DoEvents();}
 [STAThread]static void Main(){try{var work=new ConcurrentQueue<Action>();var wake=new AutoResetEvent(false);using(var network=new Network()){network.Host();using(var desk=new DeskSession(network,new Receiver(),a=>{work.Enqueue(a);wake.Set();})){desk.Host();desk.Input.Enabled=false;string root=Path.Combine(Wire.DataDir,"tls-sharing");Directory.CreateDirectory(root);using(var sender=new Client(network,Path.Combine(root,"a")))using(var receiver=new Client(network,Path.Combine(root,"b"))){var until=DateTime.UtcNow.AddSeconds(5);while(desk.Devices.Count<3&&DateTime.UtcNow<until){Pump(work);Thread.Sleep(5);}var bytes=new byte[8*1024*1024+13];new Random(119).NextBytes(bytes);string file=Path.Combine(root,"large-file.bin");File.WriteAllBytes(file,bytes);var clock=Stopwatch.StartNew();var transfer=sender.Shares.SendFiles(receiver.Id,new[]{file});until=DateTime.UtcNow.AddSeconds(60);while(!transfer.IsCompleted&&DateTime.UtcNow<until){Pump(work);wake.WaitOne(100);}Pump(work);if(!transfer.IsCompleted||receiver.Received==null||!File.ReadAllBytes(receiver.Received).SequenceEqual(bytes))throw new Exception("TLS relayed file transfer failed");if(!network.Snapshot().Any(p=>p.Id==sender.Id)||!network.Snapshot().Any(p=>p.Id==receiver.Id))throw new Exception("Transfer starved connection heartbeat");Console.WriteLine("8 MB binary file passed through two authenticated TLS clients and coordinator; "+receiver.Frames+" frames; "+clock.ElapsedMilliseconds+" ms; hash and heartbeat verified.");desk.Input.Enabled=false;}}}}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
}
