using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Velixa {
public static class Wire {
 public const int Port=37128, DiscoveryPort=37129;
 public static string Hex(byte[] b) { return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant(); }
 public static string RandomHex(int n) { byte[] b=new byte[n]; using(var r=RandomNumberGenerator.Create())r.GetBytes(b); return Hex(b); }
 public static string Clean(string s) { return s.Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant(); }
 public static string Mac(string key,string message) { using(var h=new HMACSHA256(Encoding.UTF8.GetBytes(Clean(key)))) return Hex(h.ComputeHash(Encoding.UTF8.GetBytes(message))); }
 public static bool Equal(string a,string b) { if(a==null || b==null || a.Length!=b.Length)return false; int x=0;for(int i=0;i<a.Length;i++)x|=a[i]^b[i];return x==0; }
 public static string Hash(X509Certificate cert) { using(var h=SHA256.Create())return Hex(h.ComputeHash(cert.GetRawCertData())); }
 public static string Json(object o) { return new JavaScriptSerializer().Serialize(o); }
 public static Dictionary<string,object> Parse(string s) { return new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(s); }
 public static string S(Dictionary<string,object>d,string k,string fallback="") { object v;return d.TryGetValue(k,out v)?Convert.ToString(v):fallback; }
 public static int I(Dictionary<string,object>d,string k,int fallback=0) { int x;return Int32.TryParse(S(d,k),out x)?x:fallback; }
 public static string ReadLine(StreamReader r) { var b=new StringBuilder();int c;while((c=r.Read())!=-1){if(c==10)return b.ToString();if(c!=13)b.Append((char)c);if(b.Length>16384)throw new IOException("Message too large");}throw new EndOfStreamException(); }
 public static string DataDir { get {
#if TESTING
 var test=Environment.GetEnvironmentVariable("VELIXA_TEST_DATA");if(!String.IsNullOrEmpty(test)){Directory.CreateDirectory(test);return test;}
#endif
 var p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Velixa");Directory.CreateDirectory(p);return p; } }
 public static void SaveSecret(string name,string value) { File.WriteAllBytes(Path.Combine(DataDir,name),ProtectedData.Protect(Encoding.UTF8.GetBytes(value),null,DataProtectionScope.CurrentUser)); }
 public static string LoadSecret(string name,string fallback) { try{return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(DataDir,name)),null,DataProtectionScope.CurrentUser));}catch{return fallback;} }
 public static X509Certificate2 Certificate() {
  string p=Path.Combine(DataDir,"identity.bin");
  if(File.Exists(p))try{return new X509Certificate2(ProtectedData.Unprotect(File.ReadAllBytes(p),null,DataProtectionScope.CurrentUser),"",X509KeyStorageFlags.Exportable);}catch{}
  using(var rsa=new RSACng(2048)) {
   var req=new CertificateRequest("CN=Velixa local device",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
   req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature,false));
   var cert=req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddYears(5));
   var bytes=cert.Export(X509ContentType.Pfx,"");File.WriteAllBytes(p,ProtectedData.Protect(bytes,null,DataProtectionScope.CurrentUser));
   return new X509Certificate2(bytes,"",X509KeyStorageFlags.Exportable);
  }
 }
}
public class Peer : IDisposable {
 public string Name,Kind,Id;public double X,Y;public bool Online=true;public Action<object> SendAction;public int Width=1920,Height=1080;public DateTime Seen=DateTime.UtcNow;
 public TcpClient Tcp;public SslStream Ssl;public StreamReader Reader;public StreamWriter Writer;
 public bool Closed;public Action<Peer> OnClosed;
 BlockingCollection<string> queue=new BlockingCollection<string>(512);
 public Peer() {}
 public Peer(TcpClient tcp,SslStream ssl) {Tcp=tcp;Ssl=ssl;tcp.NoDelay=true;Reader=new StreamReader(ssl,new UTF8Encoding(false),false,4096,true);Writer=new StreamWriter(ssl,new UTF8Encoding(false),4096,true){AutoFlush=true};}
 public void StartWriter() { Task.Run(()=>{try{foreach(var s in queue.GetConsumingEnumerable())Writer.WriteLine(s);}catch{}finally{Dispose();}}); }
 public void Send(object o) { if(SendAction!=null){SendAction(o);return;}if(Closed)return;try{if(!queue.TryAdd(Wire.Json(o)))Dispose();}catch{Dispose();} }
 public void Dispose() { if(Closed)return;Closed=true;queue.CompleteAdding();try{if(Tcp!=null)Tcp.Close();}catch{}if(OnClosed!=null)OnClosed(this); }
}
public class Network : IDisposable {
 public static readonly string LocalId=Identity();
 static string Identity(){string s=Wire.LoadSecret("device-id.bin","");if(s==""){s=Guid.NewGuid().ToString();Wire.SaveSecret("device-id.bin",s);}return s;}
 public string Code="",QrToken="",HostId="";public DateTime PairUntil=DateTime.MinValue;int attempts;readonly object gate=new object();
 public bool Running,IsHost;public List<Peer> Peers=new List<Peer>();public Action<Peer> Joined,Left;public Action<Peer,Dictionary<string,object>> Packet;public Action<string> Status;public Action<Dictionary<string,object>> Received;public Func<string,bool> ReconnectApproval;
 TcpListener listener;UdpClient udp;X509Certificate2 cert;Peer uplink;CancellationTokenSource stop=new CancellationTokenSource();SemaphoreSlim slots=new SemaphoreSlim(12);Dictionary<string,string> trusted=new Dictionary<string,string>();
 public Network(){try{trusted=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(Wire.LoadSecret("trusted-v2.bin","{}"));}catch{}}
 public string Fingerprint {get{return Wire.Hash(cert??Wire.Certificate());}}
 public void OpenPairing(){lock(gate){byte[] b=new byte[4];using(var r=RandomNumberGenerator.Create()){uint v;do{r.GetBytes(b);v=BitConverter.ToUInt32(b,0);}while(v>=4294960000u);Code=(v%10000).ToString("D4");}QrToken=Wire.RandomHex(32);PairUntil=DateTime.UtcNow.AddMinutes(2);attempts=0;}}
 public void ClosePairing(){lock(gate){PairUntil=DateTime.MinValue;QrToken="";Code="";}}
 public string Qr(string host){return "velixa://pair/"+Convert.ToBase64String(Encoding.UTF8.GetBytes(Wire.Json(new{v=2,host=host,id=LocalId,fp=Fingerprint,token=QrToken})));}
 public void Host(){IsHost=true;HostId=LocalId;cert=Wire.Certificate();listener=new TcpListener(IPAddress.Any,Wire.Port);listener.Start(8);Running=true;
  Task.Run(()=>{while(!stop.IsCancellationRequested)try{var t=listener.AcceptTcpClient();if(!slots.Wait(0)){t.Close();continue;}Task.Run(()=>{try{Accept(t);}finally{slots.Release();}});}catch{break;}});
  Task.Run(()=>{try{udp=new UdpClient(Wire.DiscoveryPort);while(!stop.IsCancellationRequested){IPEndPoint ep=new IPEndPoint(IPAddress.Any,0);var b=udp.Receive(ref ep);if(Encoding.UTF8.GetString(b)=="VELIXA_DISCOVER_2"){var reply=Encoding.UTF8.GetBytes("VELIXA_2|"+Environment.MachineName+"|"+LocalId);udp.Send(reply,reply.Length,ep);}}}catch{}});
  Task.Run(async()=>{while(!stop.IsCancellationRequested){await Task.Delay(1000);foreach(var p in Snapshot()){if((DateTime.UtcNow-p.Seen).TotalSeconds>5)p.Dispose();else p.Send(new{t="ping"});}}});
 }
 static byte[] Bytes(string s){return Encoding.UTF8.GetBytes(s);}
 static Org.BouncyCastle.Math.BigInteger Big(Dictionary<string,object> m,string k){string s=Wire.S(m,k);if(s.Length<1||s.Length>1024)throw new IOException("Invalid proof");return new Org.BouncyCastle.Math.BigInteger(s,16);}
 void Accept(TcpClient tcp){Peer p=null;try{
  tcp.ReceiveTimeout=8000;tcp.SendTimeout=8000;var ssl=new SslStream(tcp.GetStream(),false);ssl.AuthenticateAsServer(cert,false,SslProtocols.Tls12,false);p=new Peer(tcp,ssl);
  string nonce=Wire.RandomHex(32);p.Writer.WriteLine(Wire.Json(new{t="hello",v=2,nonce=nonce,id=LocalId}));var a=Wire.Parse(Wire.ReadLine(p.Reader));string mode=Wire.S(a,"mode");p.Id=Wire.S(a,"id");p.Name=Wire.S(a,"name","Device");p.Kind=Wire.S(a,"kind");if(p.Id.Length<8||p.Id.Length>64||p.Id==LocalId||p.Name.Length>60||(p.Kind!="Windows"&&p.Kind!="Android")||Wire.S(a,"cn").Length!=64)throw new IOException();
  string basis="Velixa-v2|"+LocalId+"|"+p.Id+"|"+Wire.Hash(cert)+"|"+nonce+"|"+Wire.S(a,"cn"),token="",pin="";
  if(mode=="resume"){lock(gate)trusted.TryGetValue(p.Id,out token);if(String.IsNullOrEmpty(token)||!Wire.Equal(Wire.S(a,"proof"),Wire.Mac(token,basis+"|client")))throw new IOException();}
  else if(mode=="qr"){lock(gate){if(DateTime.UtcNow>PairUntil||QrToken==""||!Wire.Equal(Wire.S(a,"proof"),Wire.Mac(QrToken,basis+"|client")))throw new IOException();QrToken="";PairUntil=DateTime.MinValue;}token=Wire.RandomHex(32);}
  else if(mode=="srp"&&p.Kind=="Windows"){
   lock(gate){if(DateTime.UtcNow>PairUntil||++attempts>5)throw new IOException();pin=Code;}
   var group=Org.BouncyCastle.Tls.Crypto.Srp6StandardGroups.rfc5054_2048;var random=new Org.BouncyCastle.Security.SecureRandom();byte[] salt=new byte[32];random.NextBytes(salt);
   var verifier=new Org.BouncyCastle.Crypto.Agreement.Srp.Srp6VerifierGenerator();verifier.Init(group.N,group.G,new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
   var srp=new Org.BouncyCastle.Crypto.Agreement.Srp.Srp6Server();srp.Init(group.N,group.G,verifier.GenerateVerifier(salt,Bytes(basis),Bytes(pin)),new Org.BouncyCastle.Crypto.Digests.Sha256Digest(),random);
   p.Writer.WriteLine(Wire.Json(new{t="srp",salt=Convert.ToBase64String(salt),b=srp.GenerateServerCredentials().ToString(16)}));var proof=Wire.Parse(Wire.ReadLine(p.Reader));srp.CalculateSecret(Big(proof,"a"));if(!srp.VerifyClientEvidenceMessage(Big(proof,"m")))throw new IOException();
   p.Writer.WriteLine(Wire.Json(new{t="proof",m=srp.CalculateServerEvidenceMessage().ToString(16)}));string k=srp.CalculateSessionKey().ToString(16);var confirmation=Wire.Parse(Wire.ReadLine(p.Reader));if(!Wire.Equal(Wire.S(confirmation,"proof"),Wire.Mac(k,basis+"|confirm")))throw new IOException();
   lock(gate){if(Code!=pin||DateTime.UtcNow>PairUntil)throw new IOException();ClosePairing();}token=Wire.RandomHex(32);
  }else throw new IOException();
  lock(gate){trusted[p.Id]=token;Wire.SaveSecret("trusted-v2.bin",Wire.Json(trusted));}
  p.Width=Math.Max(100,Math.Min(16000,Wire.I(a,"w",1920)));p.Height=Math.Max(100,Math.Min(16000,Wire.I(a,"h",1080)));
  foreach(var old in Snapshot())if(old.Id==p.Id)old.Dispose();lock(Peers){if(Peers.Count>=12)throw new IOException();Peers.Add(p);}p.OnClosed=q=>{lock(Peers)Peers.Remove(q);if(Left!=null)Left(q);};
  p.Writer.WriteLine(Wire.Json(new{t="ready",token=mode=="resume"?"":token,proof=Wire.Mac(token,basis+"|server")}));p.StartWriter();if(Joined!=null)Joined(p);
  while(!p.Closed){var m=Wire.Parse(Wire.ReadLine(p.Reader));p.Seen=DateTime.UtcNow;if(Wire.S(m,"t")=="size"){p.Width=Math.Max(100,Math.Min(16000,Wire.I(m,"w",1920)));p.Height=Math.Max(100,Math.Min(16000,Wire.I(m,"h",1080)));}if(Packet!=null)Packet(p,m);}
 }catch{}finally{if(p!=null)p.Dispose();else tcp.Close();}}
 public Peer[] Snapshot(){lock(Peers)return Peers.ToArray();}
 public void Send(object m){if(uplink!=null)uplink.Send(m);}
 public void Connect(string host,string pin,int width,int height){Running=true;Task.Run(async()=>{bool paired=false,declined=false,allowed=false;string token=pin.Length==4?"":Wire.LoadSecret("remote-token-v2.bin",""),fp=pin.Length==4?"":Wire.LoadSecret("remote-fp-v2.bin","");
  while(!stop.IsCancellationRequested){TcpClient tcp=null;bool reached=false;try{
   if(Status!=null)Status("Looking for your desk…");if(token!=""){string saved=Wire.LoadSecret("remote-id-v2.bin","");foreach(var d in Discover())if(d[2]==saved){host=d[0];break;}}
   tcp=new TcpClient();var connect=tcp.ConnectAsync(host,Wire.Port);if(await Task.WhenAny(connect,Task.Delay(3000))!=connect)throw new IOException("Desk is offline");await connect;
   tcp.ReceiveTimeout=8000;tcp.SendTimeout=8000;var ssl=new SslStream(tcp.GetStream(),false,(a,b,c,d)=>fp==""||Wire.Equal(fp,Wire.Hash(b)));ssl.AuthenticateAsClient("Velixa",null,SslProtocols.Tls12,false);reached=true;
   if(token!=""&&!paired&&!allowed){tcp.Close();if(declined){await Task.Delay(3000);continue;}if(ReconnectApproval!=null&&!ReconnectApproval(host)){declined=true;continue;}allowed=true;continue;}
   var p=new Peer(tcp,ssl);uplink=p;var hello=Wire.Parse(Wire.ReadLine(p.Reader));if(Wire.I(hello,"v")!=2)throw new IOException("Update Velixa on both PCs");HostId=Wire.S(hello,"id");string hash=Wire.Hash(ssl.RemoteCertificate),cn=Wire.RandomHex(32),basis="Velixa-v2|"+HostId+"|"+LocalId+"|"+hash+"|"+Wire.S(hello,"nonce")+"|"+cn;
   p.Writer.WriteLine(Wire.Json(new{mode=token==""?"srp":"resume",id=LocalId,name=Environment.MachineName,kind="Windows",cn=cn,w=width,h=height,proof=token==""?"":Wire.Mac(token,basis+"|client")}));
   if(token==""){var challenge=Wire.Parse(Wire.ReadLine(p.Reader));var group=Org.BouncyCastle.Tls.Crypto.Srp6StandardGroups.rfc5054_2048;var srp=new Org.BouncyCastle.Crypto.Agreement.Srp.Srp6Client();srp.Init(group.N,group.G,new Org.BouncyCastle.Crypto.Digests.Sha256Digest(),new Org.BouncyCastle.Security.SecureRandom());var a=srp.GenerateClientCredentials(Convert.FromBase64String(Wire.S(challenge,"salt")),Bytes(basis),Bytes(pin));srp.CalculateSecret(Big(challenge,"b"));p.Writer.WriteLine(Wire.Json(new{a=a.ToString(16),m=srp.CalculateClientEvidenceMessage().ToString(16)}));var proof=Wire.Parse(Wire.ReadLine(p.Reader));if(!srp.VerifyServerEvidenceMessage(Big(proof,"m")))throw new IOException("Pairing code does not match");p.Writer.WriteLine(Wire.Json(new{proof=Wire.Mac(srp.CalculateSessionKey().ToString(16),basis+"|confirm")}));}
   var ready=Wire.Parse(Wire.ReadLine(p.Reader));if(token=="")token=Wire.S(ready,"token");if(token.Length!=64||!Wire.Equal(Wire.S(ready,"proof"),Wire.Mac(token,basis+"|server")))throw new IOException("Pairing failed");fp=hash;Wire.SaveSecret("remote-token-v2.bin",token);Wire.SaveSecret("remote-fp-v2.bin",fp);Wire.SaveSecret("remote-id-v2.bin",HostId);Wire.SaveSecret("host.bin",host);paired=true;allowed=false;p.StartWriter();if(Status!=null)Status("Connected to your desk");while(!p.Closed&&!stop.IsCancellationRequested){var m=Wire.Parse(Wire.ReadLine(p.Reader));if(Wire.S(m,"t")=="ping")p.Send(new{t="pong"});else if(Received!=null)Received(m);}
  }catch(Exception){if(!reached)declined=false;if(Status!=null)Status(token==""?"Could not pair. Check the code and try again.":"Desk is offline · waiting nearby");if(token=="")break;}finally{paired=false;if(uplink!=null)uplink.Dispose();if(tcp!=null)tcp.Close();if(Received!=null)Received(Wire.Parse("{\"t\":\"offline\"}"));}await Task.Delay(3000);
  }
 });}
 public static List<string[]> Discover(){var found=new List<string[]>();var seen=new HashSet<string>();using(var u=new UdpClient()){u.EnableBroadcast=true;u.Client.ReceiveTimeout=300;byte[] b=Encoding.UTF8.GetBytes("VELIXA_DISCOVER_2");u.Send(b,b.Length,new IPEndPoint(IPAddress.Broadcast,Wire.DiscoveryPort));DateTime end=DateTime.UtcNow.AddSeconds(1);while(DateTime.UtcNow<end)try{var ep=new IPEndPoint(IPAddress.Any,0);var s=Encoding.UTF8.GetString(u.Receive(ref ep)).Split('|');if(s.Length==3&&s[0]=="VELIXA_2"&&seen.Add(s[2]))found.Add(new[]{ep.Address.ToString(),s[1],s[2]});}catch(SocketException){}}return found;}
 public void Dispose(){stop.Cancel();Running=false;try{if(listener!=null)listener.Stop();if(udp!=null)udp.Close();}catch{}foreach(var p in Snapshot())p.Dispose();if(uplink!=null)uplink.Dispose();}
}
}
