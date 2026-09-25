using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
namespace Velixa {
public static class WaveAudio {
 [StructLayout(LayoutKind.Sequential,Pack=2)]public struct Format{public ushort tag,channels;public uint rate,bytes;public ushort align,bits,extra;}
 [StructLayout(LayoutKind.Sequential)]public struct Header{public IntPtr data;public uint length,recorded;public UIntPtr user;public uint flags,loops;public IntPtr next;public UIntPtr reserved;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]public struct Caps{public ushort mid,pid;public uint version;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string name;public uint formats;public ushort channels,reserved;public uint support;}
 [DllImport("winmm.dll")]static extern uint waveInGetNumDevs();[DllImport("winmm.dll")]static extern uint waveOutGetNumDevs();
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern uint waveInGetDevCaps(UIntPtr id,out Caps caps,uint size);
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern uint waveOutGetDevCaps(UIntPtr id,out Caps caps,uint size);
 [DllImport("winmm.dll")]static extern uint waveInOpen(out IntPtr handle,uint id,ref Format f,IntPtr callback,IntPtr instance,uint flags);
 [DllImport("winmm.dll")]static extern uint waveOutOpen(out IntPtr handle,uint id,ref Format f,IntPtr callback,IntPtr instance,uint flags);
 [DllImport("winmm.dll")]static extern uint waveInPrepareHeader(IntPtr h,IntPtr header,uint size);[DllImport("winmm.dll")]static extern uint waveOutPrepareHeader(IntPtr h,IntPtr header,uint size);
 [DllImport("winmm.dll")]static extern uint waveInUnprepareHeader(IntPtr h,IntPtr header,uint size);[DllImport("winmm.dll")]static extern uint waveOutUnprepareHeader(IntPtr h,IntPtr header,uint size);
 [DllImport("winmm.dll")]static extern uint waveInAddBuffer(IntPtr h,IntPtr header,uint size);[DllImport("winmm.dll")]static extern uint waveOutWrite(IntPtr h,IntPtr header,uint size);
 [DllImport("winmm.dll")]static extern uint waveInStart(IntPtr h);[DllImport("winmm.dll")]static extern uint waveInReset(IntPtr h);[DllImport("winmm.dll")]static extern uint waveOutReset(IntPtr h);
 [DllImport("winmm.dll")]static extern uint waveInClose(IntPtr h);[DllImport("winmm.dll")]static extern uint waveOutClose(IntPtr h);
 public sealed class Device {public int Id;public string Name;public override string ToString(){return Name;}}
  public static Device[] Devices(bool input){var devices=new List<Device>();uint count=input?waveInGetNumDevs():waveOutGetNumDevs();for(uint i=0;i<count;i++){Caps caps;uint error=input?waveInGetDevCaps((UIntPtr)i,out caps,(uint)Marshal.SizeOf(typeof(Caps))-4):waveOutGetDevCaps((UIntPtr)i,out caps,(uint)Marshal.SizeOf(typeof(Caps)));if(error==0)devices.Add(new Device{Id=(int)i,Name=caps.name});}return devices.ToArray();}
  [DllImport("winmm.dll")]static extern uint waveInMessage(IntPtr hwi,uint uMsg,IntPtr dw1,IntPtr dw2);
  [DllImport("winmm.dll")]static extern uint waveOutMessage(IntPtr hwo,uint uMsg,IntPtr dw1,IntPtr dw2);
  const uint DRV_QUERYDEVICEINTERFACE=0x80c,DRV_QUERYDEVICEINTERFACESIZE=0x80d;
  static string QueryInterface(int id,bool input){
   try{
    IntPtr pSize=Marshal.AllocHGlobal(4);
    uint res=input?waveInMessage((IntPtr)id,DRV_QUERYDEVICEINTERFACESIZE,pSize,IntPtr.Zero):waveOutMessage((IntPtr)id,DRV_QUERYDEVICEINTERFACESIZE,pSize,IntPtr.Zero);
    if(res==0){
     int size=Marshal.ReadInt32(pSize);Marshal.FreeHGlobal(pSize);
     if(size>0){
      IntPtr pBuf=Marshal.AllocHGlobal(size*2);
      res=input?waveInMessage((IntPtr)id,DRV_QUERYDEVICEINTERFACE,pBuf,(IntPtr)size):waveOutMessage((IntPtr)id,DRV_QUERYDEVICEINTERFACE,pBuf,(IntPtr)size);
      if(res==0){string iface=Marshal.PtrToStringUni(pBuf);Marshal.FreeHGlobal(pBuf);return iface;}
      Marshal.FreeHGlobal(pBuf);
     }
    }else Marshal.FreeHGlobal(pSize);
   }catch{}
   return null;
  }
  public static Device[] FullDevices(bool input){
   var result=Devices(input);
   if(result.Length==0)return result;
   try{
    string subKey=@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\"+(input?"Capture":"Render");
    using(var root=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subKey)){
     if(root==null)return result;
     var endpoints=new List<Tuple<string,string>>();
     foreach(var id in root.GetSubKeyNames()){
      try{
       using(var dev=root.OpenSubKey(id)){
        if(dev==null)continue;
        int state=dev.GetValue("DeviceState") is int?(int)dev.GetValue("DeviceState"):-1;
        if(state!=1)continue;
        using(var props=dev.OpenSubKey("Properties")){
         if(props==null)continue;
         string friendly=props.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},14") as string;
         string epName=props.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2") as string;
         string devName=props.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6") as string;
         string btName=props.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},26") as string;
         if(!string.IsNullOrEmpty(btName))devName=btName;
         if(string.IsNullOrEmpty(friendly)){
          if(!string.IsNullOrEmpty(epName)&&!string.IsNullOrEmpty(devName))friendly=epName+" ("+devName+")";
          else if(!string.IsNullOrEmpty(devName))friendly=devName;
          else if(!string.IsNullOrEmpty(epName))friendly=epName;
         }
         string iface=props.GetValue("{233164c8-1b2c-4c7d-bc68-b671687a2567},1") as string;
         if(!string.IsNullOrEmpty(iface)&&iface.StartsWith("{2}."))iface=iface.Substring(4);
         if(!string.IsNullOrEmpty(friendly))endpoints.Add(Tuple.Create(iface??"",friendly));
        }
       }
      }catch{}
     }
     for(int i=0;i<result.Length;i++){
      string devIface=QueryInterface(result[i].Id,input);
      bool matched=false;
      if(!string.IsNullOrEmpty(devIface)){
       foreach(var ep in endpoints){
        if(!string.IsNullOrEmpty(ep.Item1)&&(ep.Item1.Equals(devIface,StringComparison.OrdinalIgnoreCase)||devIface.IndexOf(ep.Item1,StringComparison.OrdinalIgnoreCase)>=0||ep.Item1.IndexOf(devIface,StringComparison.OrdinalIgnoreCase)>=0)){
         result[i].Name=ep.Item2;matched=true;break;
        }
       }
      }
      if(!matched){
       string tr=result[i].Name;
       foreach(var ep in endpoints){
        string full=ep.Item2;
        if(full.Equals(tr,StringComparison.OrdinalIgnoreCase)||full.StartsWith(tr,StringComparison.OrdinalIgnoreCase)||tr.StartsWith(full,StringComparison.OrdinalIgnoreCase)){
         result[i].Name=full;break;
        }
       }
      }
     }
    }
   }catch{}
   if(input)foreach(var d in result){string iface=QueryInterface(d.Id,true)??"";string lower=(d.Name+" "+iface).ToLowerInvariant();string type=lower.Contains("usb")?"USB microphone":lower.Contains("array")||lower.Contains("internal")||lower.Contains("integrated")?"System microphone":"External microphone";d.Name=type+" · "+d.Name;}
   return result;
  }
 static Format Pcm(){return new Format{tag=1,channels=1,rate=48000,bytes=96000,align=2,bits=16};}
 static void Check(uint result){if(result!=0)throw new IOException("Audio device unavailable ("+result+"). Check Windows sound permissions and device selection.");}
 sealed class Buffer:IDisposable {public IntPtr Header,Data;public Buffer(int bytes){Data=Marshal.AllocHGlobal(bytes);Header=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Header)));Marshal.StructureToPtr(new Header{data=Data,length=(uint)bytes},Header,false);}public Header Read(){return (Header)Marshal.PtrToStructure(Header,typeof(Header));}public void Dispose(){Marshal.FreeHGlobal(Header);Marshal.FreeHGlobal(Data);}}
 public static void Capture(int device,Action<byte[]> chunk,CancellationToken cancel){IntPtr handle;var format=Pcm();Check(waveInOpen(out handle,(uint)device,ref format,IntPtr.Zero,IntPtr.Zero,0));var buffers=new List<Buffer>();uint headerSize=(uint)Marshal.SizeOf(typeof(Header));try{for(int i=0;i<6;i++){var buffer=new Buffer(3840);buffers.Add(buffer);Check(waveInPrepareHeader(handle,buffer.Header,headerSize));Check(waveInAddBuffer(handle,buffer.Header,headerSize));}Check(waveInStart(handle));while(!cancel.WaitHandle.WaitOne(5)){foreach(var buffer in buffers){var header=buffer.Read();if((header.flags&1)==0)continue;if(header.recorded>0){var bytes=new byte[header.recorded];Marshal.Copy(buffer.Data,bytes,0,bytes.Length);chunk(bytes);}Check(waveInAddBuffer(handle,buffer.Header,headerSize));}}}finally{waveInReset(handle);foreach(var buffer in buffers){waveInUnprepareHeader(handle,buffer.Header,headerSize);buffer.Dispose();}waveInClose(handle);}}
 public static void Play(int device,BlockingCollection<byte[]> audio,CancellationToken cancel,Action ready){IntPtr handle;var format=Pcm();Check(waveOutOpen(out handle,(uint)device,ref format,IntPtr.Zero,IntPtr.Zero,0));var buffers=new List<Buffer>();uint headerSize=(uint)Marshal.SizeOf(typeof(Header));try{ready();while(!cancel.IsCancellationRequested){for(int i=buffers.Count-1;i>=0;i--)if((buffers[i].Read().flags&1)!=0){waveOutUnprepareHeader(handle,buffers[i].Header,headerSize);buffers[i].Dispose();buffers.RemoveAt(i);}byte[] bytes;if(!audio.TryTake(out bytes,100,cancel))continue;if(buffers.Count>=6)continue;var buffer=new Buffer(bytes.Length);buffers.Add(buffer);Marshal.Copy(bytes,0,buffer.Data,bytes.Length);Check(waveOutPrepareHeader(handle,buffer.Header,headerSize));Check(waveOutWrite(handle,buffer.Header,headerSize));}}finally{waveOutReset(handle);foreach(var buffer in buffers){waveOutUnprepareHeader(handle,buffer.Header,headerSize);buffer.Dispose();}waveOutClose(handle);}}
}

 public sealed class PeerMic { public string PeerId,PeerName,MicName; public int DevId; }
 // Audio operations are injectable so routing can be tested without recording hardware.
 public interface IMicrophoneAudio {
  WaveAudio.Device[] Devices(bool input);
  void Capture(int device,Action<byte[]> chunk,CancellationToken cancel);
  void Play(int device,BlockingCollection<byte[]> audio,CancellationToken cancel,Action ready);
 }
 public sealed class SystemMicrophoneAudio:IMicrophoneAudio {
  public WaveAudio.Device[] Devices(bool input){return WaveAudio.FullDevices(input);}
  public void Capture(int device,Action<byte[]> chunk,CancellationToken cancel){WaveAudio.Capture(device,chunk,cancel);}
  public void Play(int device,BlockingCollection<byte[]> audio,CancellationToken cancel,Action ready){WaveAudio.Play(device,audio,cancel,ready);}
 }
 public sealed class MicrophoneShare:IDisposable {
  readonly Action<string,object> send;readonly Action<Action> ui;readonly Func<string,bool> available;readonly IMicrophoneAudio backend;readonly System.Windows.Forms.Timer timer;
  sealed class Sender {
   public string Peer,Id;public int Device;public bool Accepted;
   public DateTime Started=DateTime.UtcNow;public readonly CancellationTokenSource Cancel=new CancellationTokenSource();
  }
  sealed class Listener {
   public string Peer,Id,Request;public int Device;public DateTime Last=DateTime.UtcNow;
   public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public readonly BlockingCollection<byte[]> Audio=new BlockingCollection<byte[]>(6);
  }
  readonly Dictionary<string,Sender> senders=new Dictionary<string,Sender>();Listener listener;
  string requestedPeer,requestId;DateTime requestedAt,lastDiscovery=DateTime.MinValue;bool disposed;int discovering;
  public bool ReceiveEnabled,AllowRemoteRequests;public int OutputDevice=-1;public string OutputName="";
  public Action<string> Status;public string State="Choose a microphone";
  public Func<string[]> Targets=()=>new string[0],DiscoveryTargets=()=>new string[0];
  public readonly ConcurrentDictionary<string,List<PeerMic>> RemoteMics=new ConcurrentDictionary<string,List<PeerMic>>();
  public Action MicsChanged;
  public bool Sending{get{return senders.Count>0;}}
  public bool Receiving{get{return listener!=null&&!listener.Cancel.IsCancellationRequested;}}
  public bool Pending{get{return requestId!=null;}}
  public MicrophoneShare(Action<string,object> transport,Action<Action> dispatch,Func<string,bool> reachable,IMicrophoneAudio audioBackend=null){send=transport;ui=dispatch;available=reachable;backend=audioBackend??new SystemMicrophoneAudio();timer=new System.Windows.Forms.Timer{Interval=1000};timer.Tick+=(s,e)=>Tick();timer.Start();}
  void Emit(string peer,object message){if(!disposed)send(peer,message);}
  void Update(string state){if(disposed)return;State=state;if(Status!=null)Status(state);}
  public static bool IsCable(WaveAudio.Device d){return d.Name.IndexOf("CABLE Input",StringComparison.OrdinalIgnoreCase)>=0;}
  public int ResolveOutput(){var outputs=backend.Devices(false);WaveAudio.Device selected=null;if(OutputName!="")selected=outputs.FirstOrDefault(d=>d.Name==OutputName);else if(OutputDevice>=0)selected=outputs.FirstOrDefault(d=>d.Id==OutputDevice);else selected=outputs.FirstOrDefault(IsCable);return selected==null?-1:selected.Id;}
  void Tick(){
   foreach(var s in senders.Values.ToArray())if(!available(s.Peer)||(!s.Accepted&&(DateTime.UtcNow-s.Started).TotalSeconds>10))StopSender(s);
   if(listener!=null&&listener.Peer!=""&&(!available(listener.Peer)||(DateTime.UtcNow-listener.Last).TotalSeconds>5)){StopReceiving();Update("Microphone disconnected — choose a source to reconnect");}
   if(Pending&&(DateTime.UtcNow-requestedAt).TotalSeconds>10){StopReceiving();Update("Microphone did not respond. Check that Velixa is running on the source.");}
   bool changed=false;foreach(var key in RemoteMics.Keys)if(!available(key)){List<PeerMic> unused;changed|=RemoteMics.TryRemove(key,out unused);}if(changed&&MicsChanged!=null)MicsChanged();
   if((DateTime.UtcNow-lastDiscovery).TotalSeconds<5||Interlocked.Exchange(ref discovering,1)!=0)return;lastDiscovery=DateTime.UtcNow;
   var peers=DiscoveryTargets();foreach(var peer in peers)Emit(peer,new{kind="mic-list-request",id=Guid.NewGuid().ToString("N")});
   Discover(peers);
  }
  void Discover(string[] peers){Task.Run(()=>{try{var mics=backend.Devices(true).Select(d=>new{id=d.Id,name=d.Name}).ToArray();ui(()=>{if(disposed)return;foreach(var peer in peers)if(available(peer))Emit(peer,new{kind="mic-list",id=Guid.NewGuid().ToString("N"),peerName=Environment.MachineName,mics=mics});});}finally{Interlocked.Exchange(ref discovering,0);}});}
  public void BroadcastMics(string peer=null){if(peer!=null)Discover(new[]{peer});}
  public bool RequestRemoteMic(string peer,int devId,string micName=null){
   StopReceiving();int output=ResolveOutput();if(output<0){Update("Set up an app microphone output first (virtual audio cable)");return false;}
   if(!available(peer)){Update("That microphone device is offline");return false;}
   ReceiveEnabled=true;requestedPeer=peer;requestId=Guid.NewGuid().ToString("N");requestedAt=DateTime.UtcNow;
   Update("Connecting microphone…");Emit(peer,new{kind="mic-request",id=requestId,devId=devId,micName=micName});return true;
  }
  public bool SelectLocal(int device){
   StopReceiving();int output=ResolveOutput();if(output<0){Update("Set up an app microphone output first (virtual audio cable)");return false;}
   if(!backend.Devices(true).Any(d=>d.Id==device)){Update("Microphone was unplugged. Choose another source.");return false;}
   var run=new Listener{Peer="",Id=Guid.NewGuid().ToString("N"),Device=output};listener=run;Update("Connecting microphone…");
   StartPlayback(run,()=>Task.Run(()=>{try{backend.Capture(device,bytes=>QueueAudio(run,bytes),run.Cancel.Token);}catch(OperationCanceledException){}catch(Exception e){ui(()=>FailListener(run,e.Message));}}));return true;
  }
  void QueueAudio(Listener run,byte[] bytes){if(run.Cancel.IsCancellationRequested)return;if(!run.Audio.TryAdd(bytes)){byte[] old;run.Audio.TryTake(out old);run.Audio.TryAdd(bytes);}}
  void StartPlayback(Listener run,Action ready){Task.Run(()=>{try{backend.Play(run.Device,run.Audio,run.Cancel.Token,()=>ui(()=>{if(disposed||listener!=run||run.Cancel.IsCancellationRequested)return;Update("Microphone ready for apps");ready();}));}catch(OperationCanceledException){}catch(Exception e){ui(()=>FailListener(run,e.Message));}finally{ui(()=>{if(!disposed&&listener==run&&!run.Cancel.IsCancellationRequested)FailListener(run,"Microphone output stopped");});}});}
  void FailListener(Listener run,string message){if(disposed||listener!=run)return;StopReceiving();Update(message);}
  public void Start(string peer,int device){StartMany(new[]{peer},device);}
  public void StartMany(IEnumerable<string> peers,int device){foreach(var peer in peers.Where(available).Distinct())StartSender(peer,device,null);}
  void StartSender(string peer,int device,string request){
   Sender old;if(senders.TryGetValue(peer,out old))StopSender(old);
   var run=new Sender{Peer=peer,Id=request??Guid.NewGuid().ToString("N"),Device=device};senders[peer]=run;
   Emit(peer,new{kind="mic-begin",id=run.Id,request=request,rate=48000,channels=1,bits=16});Update("Microphone requested by a paired PC");
  }
  bool Current(Sender run){Sender current;return !disposed&&senders.TryGetValue(run.Peer,out current)&&current==run&&!run.Cancel.IsCancellationRequested;}
  void Capture(Sender run){Task.Run(()=>{try{backend.Capture(run.Device,bytes=>{if(run.Cancel.IsCancellationRequested)return;string data=Convert.ToBase64String(bytes);ui(()=>{if(Current(run))Emit(run.Peer,new{kind="mic-data",id=run.Id,data=data});});},run.Cancel.Token);}catch(OperationCanceledException){}catch(Exception e){ui(()=>{if(Current(run)){Emit(run.Peer,new{kind="mic-error",id=run.Id,message=e.Message});StopSender(run);Update(e.Message);}});}finally{ui(()=>{if(Current(run))StopSender(run);});}});}
  void StopSender(Sender run){Sender current;if(!senders.TryGetValue(run.Peer,out current)||current!=run)return;senders.Remove(run.Peer);run.Cancel.Cancel();Emit(run.Peer,new{kind="mic-stop",id=run.Id});Update(Receiving?"Microphone ready for apps":Pending?"Connecting microphone…":Sending?"Sharing microphone with a paired PC":"Microphone off");}
  public void Handle(string from,Dictionary<string,object> m){
   string kind=Wire.S(m,"kind"),id=Wire.S(m,"id");Guid guid;if(disposed||!available(from)||!Guid.TryParseExact(id,"N",out guid))return;
   if(kind=="mic-list-request"){BroadcastMics(from);return;}
   if(kind=="mic-list"){object raw;var list=new List<PeerMic>();if(m.TryGetValue("mics",out raw)&&raw is System.Collections.IEnumerable){foreach(var item in (System.Collections.IEnumerable)raw){var d=item as Dictionary<string,object>;if(d==null||list.Count>=128)continue;string name=Wire.S(d,"name");if(name.Length>160)name=name.Substring(0,160);list.Add(new PeerMic{PeerId=from,PeerName=Wire.S(m,"peerName","PC"),DevId=Wire.I(d,"id"),MicName=name});}}RemoteMics[from]=list;if(MicsChanged!=null)MicsChanged();return;}
   if(kind=="mic-request"){
    if(!AllowRemoteRequests){Emit(from,new{kind="mic-error",id=id,message="Microphone access is disabled on the source PC"});return;}
    int dev=Wire.I(m,"devId",-1);if(!backend.Devices(true).Any(d=>d.Id==dev&&(Wire.S(m,"micName")==""||d.Name==Wire.S(m,"micName")))){Emit(from,new{kind="mic-error",id=id,message="Microphone was unplugged. Refresh the list."});return;}
    StartSender(from,dev,id);return;
   }
   if(kind=="mic-error"){if((from==requestedPeer&&id==requestId)||(listener!=null&&from==listener.Peer&&id==listener.Id)){StopReceiving();Update(Wire.S(m,"message","Microphone unavailable"));}return;}
   Sender target;
   if(kind=="mic-accept"&&senders.TryGetValue(from,out target)&&id==target.Id){if(Wire.S(m,"ok")!="True"){StopSender(target);return;}if(!target.Accepted){target.Accepted=true;Capture(target);}return;}
   if(kind=="mic-stop"){if(listener!=null&&from==listener.Peer&&id==listener.Id){StopReceiving();Update("Source microphone stopped");}if(senders.TryGetValue(from,out target)&&id==target.Id)StopSender(target);return;}
   if(kind=="mic-begin"){
    // A selection on this PC authorizes exactly one source. Never play unsolicited audio.
    string request=Wire.S(m,"request");int device=ResolveOutput();
    if(!ReceiveEnabled||from!=requestedPeer||!Pending||(request!=""&&request!=requestId)||device<0||Receiving||Wire.I(m,"rate")!=48000||Wire.I(m,"channels")!=1||Wire.I(m,"bits")!=16){Emit(from,new{kind="mic-accept",id=id,ok=false});return;}
    var run=new Listener{Peer=from,Id=id,Request=requestId,Device=device};listener=run;requestId=null;requestedPeer=null;
    StartPlayback(run,()=>Emit(from,new{kind="mic-accept",id=id,ok=true}));return;
   }
   if(kind=="mic-data"&&listener!=null&&from==listener.Peer&&id==listener.Id){string data=Wire.S(m,"data");if(data.Length>5120)return;try{var bytes=Convert.FromBase64String(data);if(bytes.Length==0||bytes.Length>3840||bytes.Length%2!=0)return;listener.Last=DateTime.UtcNow;QueueAudio(listener,bytes);}catch(FormatException){}}
  }
  public void StopSending(){foreach(var s in senders.Values.ToArray())StopSender(s);if(!Receiving&&!Pending)Update("Choose a microphone");}
  public void StopReceiving(){var pendingPeer=requestedPeer;var pendingId=requestId;requestId=null;requestedPeer=null;if(pendingPeer!=null)Emit(pendingPeer,new{kind="mic-stop",id=pendingId});var run=listener;listener=null;if(run!=null){run.Cancel.Cancel();if(run.Peer!="")Emit(run.Peer,new{kind="mic-stop",id=run.Id});Update(Receiving?"Microphone ready for apps":Pending?"Connecting microphone…":Sending?"Sharing microphone with a paired PC":"Microphone off");}Update(Sending?"Sharing microphone with a paired PC":"Microphone off");}
  public void Dispose(){StopSending();StopReceiving();disposed=true;timer.Dispose();}
 }
}
