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
 public static Device[] FullDevices(bool input){
  var result=Devices(input);
  if(result.Length==0)return result;
  try{
   string subKey=@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\"+(input?"Capture":"Render");
   using(var root=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subKey)){
    if(root==null)return result;
    var fullNames=new List<string>();
    foreach(var id in root.GetSubKeyNames()){
     try{
      using(var dev=root.OpenSubKey(id+@"\Properties")){
       if(dev==null)continue;
       string friendly=dev.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},14") as string;
       string epName=dev.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2") as string;
       string devName=dev.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6") as string;
       if(string.IsNullOrEmpty(friendly)){
        if(!string.IsNullOrEmpty(epName)&&!string.IsNullOrEmpty(devName))friendly=epName+" ("+devName+")";
        else if(!string.IsNullOrEmpty(devName))friendly=devName;
        else if(!string.IsNullOrEmpty(epName))friendly=epName;
       }
       if(!string.IsNullOrEmpty(friendly))fullNames.Add(friendly);
      }
     }catch{}
    }
    for(int i=0;i<result.Length;i++){
     string tr=result[i].Name;
     foreach(var full in fullNames){
      if(full.IndexOf(tr,StringComparison.OrdinalIgnoreCase)>=0||tr.IndexOf(full,StringComparison.OrdinalIgnoreCase)>=0||(tr.Length>8&&full.StartsWith(tr.Substring(0,8),StringComparison.OrdinalIgnoreCase))){
       result[i].Name=full;
       break;
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
public sealed class MicrophoneShare:IDisposable {
 readonly Action<string,object> send;readonly Action<Action> ui;readonly Func<string,bool> available;readonly System.Windows.Forms.Timer timer;CancellationTokenSource capture,playback;BlockingCollection<byte[]> audio;TaskCompletionSource<bool> accepted;string sendPeer,sendId,receivePeer,receiveId;DateTime lastAudio;bool disposed;public bool ReceiveEnabled;public int OutputDevice=-1;public Action<string> Status;public string State="Microphone is off";
 public bool Sending{get{return capture!=null&&!capture.IsCancellationRequested;}}public bool Receiving{get{return playback!=null&&!playback.IsCancellationRequested;}}
 public MicrophoneShare(Action<string,object> transport,Action<Action> dispatch,Func<string,bool> reachable){send=transport;ui=dispatch;available=reachable;timer=new System.Windows.Forms.Timer{Interval=1000};timer.Tick+=(s,e)=>{if(Sending&&!available(sendPeer))StopSending();if(Receiving&&(!ReceiveEnabled||!available(receivePeer)||(DateTime.UtcNow-lastAudio).TotalSeconds>5))StopReceiving();};timer.Start();}
 void Emit(string peer,object message){ui(()=>{if(!disposed)send(peer,message);});}
 void Update(string state){ui(()=>{if(disposed)return;State=state;if(Status!=null)Status(state);});}
 public void Start(string peer,int device){StopSending();if(!available(peer)){Update("Choose a connected Windows PC with sharing support");return;}capture=new CancellationTokenSource();var token=capture.Token;string id=Guid.NewGuid().ToString("N");sendPeer=peer;sendId=id;var ready=new TaskCompletionSource<bool>();accepted=ready;Update("Waiting for the receiving PC’s audio output");Task.Run(()=>{try{Emit(peer,new{kind="mic-begin",id=id,rate=48000,channels=1,bits=16});if(!ready.Task.Wait(10000,token)||!ready.Task.Result)throw new IOException("Enable microphone receiving and select an output on the other PC");Update("Microphone sharing is on");WaveAudio.Capture(device,bytes=>Emit(peer,new{kind="mic-data",id=id,data=Convert.ToBase64String(bytes)}),token);}catch(OperationCanceledException){}catch(Exception e){Update(e.Message);}finally{Emit(peer,new{kind="mic-stop",id=id});ui(()=>{if(sendId==id&&capture!=null)capture.Cancel();});}});}
 public void Handle(string from,Dictionary<string,object> m){string kind=Wire.S(m,"kind"),id=Wire.S(m,"id");Guid guid;if(!Guid.TryParseExact(id,"N",out guid))return;if(kind=="mic-accept"&&from==sendPeer&&id==sendId){if(accepted!=null)accepted.TrySetResult(Wire.S(m,"ok")=="True");return;}if(kind=="mic-stop"){if(from==receivePeer&&id==receiveId)StopReceiving();if(from==sendPeer&&id==sendId)StopSending();return;}if(kind=="mic-begin"){if(!ReceiveEnabled||OutputDevice<0||Receiving||Wire.I(m,"rate")!=48000||Wire.I(m,"channels")!=1||Wire.I(m,"bits")!=16){Emit(from,new{kind="mic-accept",id=id,ok=false});return;}receivePeer=from;receiveId=id;lastAudio=DateTime.UtcNow;audio=new BlockingCollection<byte[]>(6);playback=new CancellationTokenSource();var queue=audio;var cancel=playback.Token;int device=OutputDevice;Task.Run(()=>{try{WaveAudio.Play(device,queue,cancel,()=>{Emit(from,new{kind="mic-accept",id=id,ok=true});Update("Receiving shared microphone");});}catch(OperationCanceledException){}catch(Exception e){Emit(from,new{kind="mic-accept",id=id,ok=false});Update(e.Message);}finally{ui(()=>{if(receiveId==id&&playback!=null)playback.Cancel();});}});return;}if(kind=="mic-data"&&ReceiveEnabled&&Receiving&&from==receivePeer&&id==receiveId){string data=Wire.S(m,"data");if(data.Length>6000)return;try{var bytes=Convert.FromBase64String(data);if(bytes.Length==0||bytes.Length>3840||bytes.Length%2!=0)return;lastAudio=DateTime.UtcNow;if(!audio.TryAdd(bytes)){byte[] old;audio.TryTake(out old);audio.TryAdd(bytes);}}catch(FormatException){}}}
 public void StopSending(){if(capture!=null&&!capture.IsCancellationRequested){capture.Cancel();if(sendPeer!=null)Emit(sendPeer,new{kind="mic-stop",id=sendId});}Update("Microphone is off");}
 public void StopReceiving(){if(playback!=null&&!playback.IsCancellationRequested){playback.Cancel();if(receivePeer!=null)Emit(receivePeer,new{kind="mic-stop",id=receiveId});}Update("Microphone is off");}
 public void Dispose(){StopSending();StopReceiving();disposed=true;timer.Dispose();}
}
}
