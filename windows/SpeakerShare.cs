using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Wave;
namespace Velixa {
public sealed class SpeakerShare:IDisposable {
 readonly Action<string,object> send;readonly Action<Action> ui;readonly Func<DeskSession> desk;readonly Func<bool> enabled;readonly Func<string> outputName;
 ProcessAudio capture;string target="",stream="";int generation,pending;bool disposed;DateTime lastReady=DateTime.MinValue,lastProbe=DateTime.MinValue;
 public Action Changed;public bool Ready{get{return capture!=null&&(DateTime.UtcNow-lastReady).TotalSeconds<6;}}
 sealed class Playback:IDisposable{public string Id,Output;public BufferedWaveProvider Buffer;public WaveOutEvent Player;public DateTime Seen;public void Dispose(){Player.Stop();Player.Dispose();}}
 readonly Dictionary<string,Playback> players=new Dictionary<string,Playback>();
 public SpeakerShare(Action<string,object> transport,Action<Action> dispatch,Func<DeskSession> session,Func<bool> active,Func<string> output){send=transport;ui=dispatch;desk=session;enabled=active;outputName=output;}
 public void Reconcile(){
  if(disposed)return;var session=desk();string next=enabled()&&session.Connected?session.SpeakerTarget(Network.LocalId):"";if(next==Network.LocalId)next="";
  foreach(var key in players.Keys.ToArray())if(!enabled()||session.SpeakerTarget(key)!=Network.LocalId||session.Devices.All(d=>d.id!=key||!d.Available)||players[key].Output!=ResolvedOutput()||players[key].Player.PlaybackState==PlaybackState.Stopped||(DateTime.UtcNow-players[key].Seen).TotalSeconds>8){players[key].Dispose();players.Remove(key);}
  if(next==target&&capture!=null){capture.Check();Probe();return;}StopCapture();if(next=="")return;
  target=next;stream=Guid.NewGuid().ToString("N");int version=++generation;string destination=target,id=stream;
  capture=new ProcessAudio(bytes=>{
   if(version!=generation)return;
   // The transport is mono PCM16. Downmix in bounded chunks; never accumulate
   // unbounded UI work when the desktop thread is temporarily busy.
   for(int offset=0;offset<bytes.Length;offset+=7680){int length=Math.Min(7680,bytes.Length-offset);byte[] mono=new byte[length/2];for(int i=0;i<length;i+=4){short value=(short)(((int)BitConverter.ToInt16(bytes,offset+i)+BitConverter.ToInt16(bytes,offset+i+2))/2);mono[i/2]=(byte)value;mono[i/2+1]=(byte)(value>>8);}if(Interlocked.Increment(ref pending)>12){Interlocked.Decrement(ref pending);continue;}string data=Convert.ToBase64String(mono);ui(()=>{try{if(!disposed&&version==generation)send(destination,new{kind="speaker-data",id=id,data=data});}finally{Interlocked.Decrement(ref pending);}});}
  });Probe();
 }
 void Probe(){if((DateTime.UtcNow-lastProbe).TotalSeconds<2)return;lastProbe=DateTime.UtcNow;send(target,new{kind="speaker-start",id=stream});}
 public void Handle(string from,Dictionary<string,object> message){
  if(disposed||!enabled())return;string id=Wire.S(message,"id"),kind=Wire.S(message,"kind");Guid guid;if(!Guid.TryParseExact(id,"N",out guid))return;
  if(kind=="speaker-ready"){if(from!=target||id!=stream||desk().SpeakerTarget(Network.LocalId)!=from)return;bool wasReady=Ready;lastReady=DateTime.UtcNow;if(!wasReady&&Changed!=null)Changed();return;}
  if(desk().SpeakerTarget(from)!=Network.LocalId)return;
  Playback current;players.TryGetValue(from,out current);
  if(kind=="speaker-stop"){if(current!=null&&current.Id==id){current.Dispose();players.Remove(from);}return;}
  if(kind!="speaker-start"&&kind!="speaker-data")return;
  byte[] data=new byte[0];if(kind=="speaker-data"){try{string text=Wire.S(message,"data");if(text.Length>5120)return;data=Convert.FromBase64String(text);}catch{return;}if(data.Length==0||data.Length>3840||data.Length%2!=0)return;}
  if(current==null||current.Id!=id||current.Player.PlaybackState==PlaybackState.Stopped){
   if(current!=null){current.Dispose();players.Remove(from);}var devices=WaveAudio.FullDevices(false);string output=ResolvedOutput();var selected=devices.FirstOrDefault(d=>d.Name==output)??devices.FirstOrDefault(d=>d.Name.IndexOf("CABLE",StringComparison.OrdinalIgnoreCase)<0);if(selected==null)return;
   current=new Playback{Id=id,Output=output,Buffer=new BufferedWaveProvider(new WaveFormat(48000,16,1)){BufferDuration=TimeSpan.FromMilliseconds(250),DiscardOnBufferOverflow=true},Player=new WaveOutEvent{DeviceNumber=selected.Id,DesiredLatency=100}};
   try{current.Player.Init(current.Buffer);current.Player.Play();players[from]=current;}catch{current.Dispose();throw;}
  }
  current.Seen=DateTime.UtcNow;if(data.Length>0)current.Buffer.AddSamples(data,0,data.Length);
  if(kind=="speaker-start")send(from,new{kind="speaker-ready",id=id});
 }
 string ResolvedOutput(){string selected=outputName();return selected!=""&&WaveAudio.FullDevices(false).Any(d=>d.Name==selected)?selected:AudioEndpoints.LocalDefaultName(false);}
 void StopCapture(){++generation;lastReady=lastProbe=DateTime.MinValue;var old=capture;capture=null;if(old!=null)old.Dispose();if(target!="")send(target,new{kind="speaker-stop",id=stream});target="";}
 public void Stop(){StopCapture();foreach(var player in players.Values)player.Dispose();players.Clear();}
 public void Dispose(){Stop();disposed=true;}
}
}


