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
   return result;
  }
 static Format Pcm(){return new Format{tag=1,channels=1,rate=48000,bytes=96000,align=2,bits=16};}
 static void Check(uint result){if(result!=0)throw new IOException("Audio device unavailable ("+result+"). Check Windows sound permissions and device selection.");}
 sealed class Buffer:IDisposable {public IntPtr Header,Data;public Buffer(int bytes){Data=Marshal.AllocHGlobal(bytes);Header=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Header)));Marshal.StructureToPtr(new Header{data=Data,length=(uint)bytes},Header,false);}public Header Read(){return (Header)Marshal.PtrToStructure(Header,typeof(Header));}public void Dispose(){Marshal.FreeHGlobal(Header);Marshal.FreeHGlobal(Data);}}
 public static void Capture(int device,Action<byte[]> chunk,CancellationToken cancel){IntPtr handle;var format=Pcm();Check(waveInOpen(out handle,(uint)device,ref format,IntPtr.Zero,IntPtr.Zero,0));var buffers=new List<Buffer>();uint headerSize=(uint)Marshal.SizeOf(typeof(Header));try{for(int i=0;i<6;i++){var buffer=new Buffer(3840);buffers.Add(buffer);Check(waveInPrepareHeader(handle,buffer.Header,headerSize));Check(waveInAddBuffer(handle,buffer.Header,headerSize));}Check(waveInStart(handle));while(!cancel.WaitHandle.WaitOne(5)){foreach(var buffer in buffers){var header=buffer.Read();if((header.flags&1)==0)continue;if(header.recorded>0){var bytes=new byte[header.recorded];Marshal.Copy(buffer.Data,bytes,0,bytes.Length);chunk(bytes);}Check(waveInAddBuffer(handle,buffer.Header,headerSize));}}}finally{waveInReset(handle);foreach(var buffer in buffers){waveInUnprepareHeader(handle,buffer.Header,headerSize);buffer.Dispose();}waveInClose(handle);}}
 public static void Play(int device,BlockingCollection<byte[]> audio,CancellationToken cancel,Action ready){IntPtr handle;var format=Pcm();Check(waveOutOpen(out handle,(uint)device,ref format,IntPtr.Zero,IntPtr.Zero,0));var buffers=new List<Buffer>();uint headerSize=(uint)Marshal.SizeOf(typeof(Header));try{ready();while(!cancel.IsCancellationRequested){for(int i=buffers.Count-1;i>=0;i--)if((buffers[i].Read().flags&1)!=0){waveOutUnprepareHeader(handle,buffers[i].Header,headerSize);buffers[i].Dispose();buffers.RemoveAt(i);}byte[] bytes;if(!audio.TryTake(out bytes,100,cancel))continue;if(buffers.Count>=6)continue;var buffer=new Buffer(bytes.Length);buffers.Add(buffer);Marshal.Copy(bytes,0,buffer.Data,bytes.Length);Check(waveOutPrepareHeader(handle,buffer.Header,headerSize));Check(waveOutWrite(handle,buffer.Header,headerSize));}}finally{waveOutReset(handle);foreach(var buffer in buffers){waveOutUnprepareHeader(handle,buffer.Header,headerSize);buffer.Dispose();}waveOutClose(handle);}}
}
 public sealed class PeerMic { public string PeerId,PeerName,MicName; public int DevId; }
 public sealed class MicrophoneShare:IDisposable {
  readonly Action<string,object> send;readonly Action<Action> ui;readonly Func<string,bool> available;readonly System.Windows.Forms.Timer timer;CancellationTokenSource capture,playback;BlockingCollection<byte[]> audio;TaskCompletionSource<bool> accepted;string sendPeer,sendId,receivePeer,receiveId;DateTime lastAudio;bool disposed;public bool ReceiveEnabled;public int OutputDevice=-1;public Action<string> Status;public string State="Microphone is off";
  public readonly ConcurrentDictionary<string,List<PeerMic>> RemoteMics=new ConcurrentDictionary<string,List<PeerMic>>();
  public Action MicsChanged;
  public bool Sending{get{return capture!=null&&!capture.IsCancellationRequested;}}public bool Receiving{get{return playback!=null&&!playback.IsCancellationRequested;}}
  public MicrophoneShare(Action<string,object> transport,Action<Action> dispatch,Func<string,bool> reachable){send=transport;ui=dispatch;available=reachable;timer=new System.Windows.Forms.Timer{Interval=1000};timer.Tick+=(s,e)=>{if(Sending&&!available(sendPeer))StopSending();if(Receiving&&(!ReceiveEnabled||!available(receivePeer)||(DateTime.UtcNow-lastAudio).TotalSeconds>5))StopReceiving();};timer.Start();}
  void Emit(string peer,object message){ui(()=>{if(!disposed)send(peer,message);});}
  void Update(string state){ui(()=>{if(disposed)return;State=state;if(Status!=null)Status(state);});}
  public void BroadcastMics(string peer=null){var mics=WaveAudio.FullDevices(true).Select(d=>new{id=d.Id,name=d.Name}).ToArray();var body=new{kind="mic-list",id=Guid.NewGuid().ToString("N"),peerName=Environment.MachineName,mics=mics};if(peer!=null)Emit(peer,body);}
  public void RequestRemoteMic(string peer,int devId){StopSending();Emit(peer,new{kind="mic-request",id=Guid.NewGuid().ToString("N"),devId=devId});Update("Connecting to microphone…");}
  public void Start(string peer,int device){StopSending();if(!available(peer)){Update("Choose a connected Windows PC with sharing support");return;}capture=new CancellationTokenSource();var token=capture.Token;string id=Guid.NewGuid().ToString("N");sendPeer=peer;sendId=id;var ready=new TaskCompletionSource<bool>();accepted=ready;Update("Waiting for the receiving PC’s audio output");Task.Run(()=>{try{Emit(peer,new{kind="mic-begin",id=id,rate=48000,channels=1,bits=16});if(!ready.Task.Wait(10000,token)||!ready.Task.Result)throw new IOException("Enable microphone receiving and select an output on the other PC");Update("Microphone sharing is on");WaveAudio.Capture(device,bytes=>Emit(peer,new{kind="mic-data",id=id,data=Convert.ToBase64String(bytes)}),token);}catch(OperationCanceledException){}catch(Exception e){Update(e.Message);}finally{Emit(peer,new{kind="mic-stop",id=id});ui(()=>{if(sendId==id&&capture!=null)capture.Cancel();});}});}
  public void Handle(string from,Dictionary<string,object> m){string kind=Wire.S(m,"kind"),id=Wire.S(m,"id");Guid guid;if(!Guid.TryParseExact(id,"N",out guid))return;if(kind=="mic-list"){object listObj;var list=new List<PeerMic>();string peerName=Wire.S(m,"peerName","PC");if(m.TryGetValue("mics",out listObj)&&listObj is object[]){foreach(var item in (object[])listObj){if(item is Dictionary<string,object>){var d=(Dictionary<string,object>)item;list.Add(new PeerMic{PeerId=from,PeerName=peerName,DevId=Wire.I(d,"id"),MicName=Wire.S(d,"name")});}}}RemoteMics[from]=list;if(MicsChanged!=null)ui(MicsChanged);return;}if(kind=="mic-request"){int reqDev=Wire.I(m,"devId");Start(from,reqDev);return;}if(kind=="mic-accept"&&from==sendPeer&&id==sendId){if(accepted!=null)accepted.TrySetResult(Wire.S(m,"ok")=="True");return;}if(kind=="mic-stop"){if(from==receivePeer&&id==receiveId)StopReceiving();if(from==sendPeer&&id==sendId)StopSending();return;}if(kind=="mic-begin"){int device=OutputDevice>=0?OutputDevice:(WaveAudio.Devices(false).Length>0?0:-1);if(!ReceiveEnabled||device<0||Receiving||Wire.I(m,"rate")!=48000||Wire.I(m,"channels")!=1||Wire.I(m,"bits")!=16){Emit(from,new{kind="mic-accept",id=id,ok=false});return;}receivePeer=from;receiveId=id;lastAudio=DateTime.UtcNow;audio=new BlockingCollection<byte[]>(6);playback=new CancellationTokenSource();var queue=audio;var cancel=playback.Token;Task.Run(()=>{try{WaveAudio.Play(device,queue,cancel,()=>{Emit(from,new{kind="mic-accept",id=id,ok=true});Update("Receiving shared microphone");});}catch(OperationCanceledException){}catch(Exception e){Emit(from,new{kind="mic-accept",id=id,ok=false});Update(e.Message);}finally{ui(()=>{if(receiveId==id&&playback!=null)playback.Cancel();});}});return;}if(kind=="mic-data"&&ReceiveEnabled&&Receiving&&from==receivePeer&&id==receiveId){string data=Wire.S(m,"data");if(data.Length>6000)return;try{var bytes=Convert.FromBase64String(data);if(bytes.Length==0||bytes.Length>3840||bytes.Length%2!=0)return;lastAudio=DateTime.UtcNow;if(!audio.TryAdd(bytes)){byte[] old;audio.TryTake(out old);audio.TryAdd(bytes);}}catch(FormatException){}}}
  public void StopSending(){if(capture!=null&&!capture.IsCancellationRequested){capture.Cancel();if(sendPeer!=null)Emit(sendPeer,new{kind="mic-stop",id=sendId});}Update("Microphone is off");}
  public void StopReceiving(){if(playback!=null&&!playback.IsCancellationRequested){playback.Cancel();if(receivePeer!=null)Emit(receivePeer,new{kind="mic-stop",id=receiveId});}Update("Microphone is off");}
  public void Dispose(){StopSending();StopReceiving();disposed=true;timer.Dispose();}
 }
}
