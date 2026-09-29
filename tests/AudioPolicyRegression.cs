using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using Velixa;
class AudioPolicyRegression {
 static Dictionary<string,bool> Snapshot(){var values=new Dictionary<string,bool>();using(var devices=new MMDeviceEnumerator())foreach(var d in devices.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active))using(d)if(d.FriendlyName.IndexOf("CABLE",StringComparison.OrdinalIgnoreCase)<0)values[d.ID]=d.AudioEndpointVolume.Mute;return values;}
 static bool Same(Dictionary<string,bool> expected){var actual=Snapshot();return expected.All(p=>actual.ContainsKey(p.Key)&&actual[p.Key]==p.Value);}
 static void Check(bool value,string text){if(!value)throw new Exception(text);Console.WriteLine("PASS "+text);}
 static void Main(string[] args){if(args.Length==2&&args[0]=="--audio-guard"){try{Process.GetProcessById(Int32.Parse(args[1])).WaitForExit();}catch{}if(Wire.LoadSecret("audio-owner.bin","")==args[1])AudioEndpoints.RestoreAndHide();return;}if(args.Length==1&&args[0]=="--crash"){AudioEndpoints.SuppressLocalSpeakers();Environment.Exit(17);return;}var original=Snapshot();try{Check(original.Count>0,"physical output available for policy verification");AudioEndpoints.SuppressLocalSpeakers();Check(Snapshot().All(p=>p.Value),"all physical outputs muted during remote routing");Check(Wire.LoadSecret("audio-render-mutes.bin","{}").Length>2,"original mute states journaled before mutation");AudioEndpoints.RestoreLocalSpeakers();Check(Same(original),"stopping restores exact previous mute states");using(var child=Process.Start(new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath,"--crash"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden})){if(!child.WaitForExit(15000))throw new Exception("Crash harness timeout");Check(child.ExitCode==17,"audio owner exited without cleanup");}DateTime until=DateTime.UtcNow.AddSeconds(10);while(DateTime.UtcNow<until&&(Wire.LoadSecret("audio-render-mutes.bin","{}")!="{}"||!Same(original)))Thread.Sleep(100);Check(Same(original)&&Wire.LoadSecret("audio-render-mutes.bin","{}") == "{}","independent guard restores audio after abrupt exit");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}finally{AudioEndpoints.RestoreLocalSpeakers();}}
}
