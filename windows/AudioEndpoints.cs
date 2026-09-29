using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
namespace Velixa {
// Windows audio policy adapter. Keep the recovery journal until defaults are restored.
public static class AudioEndpoints {
 [ComImport,Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]class PolicyClient{}
 [ComImport,Guid("F8679F50-850A-41CF-9C72-430F290290C8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IPolicy {
  void GetMixFormat();void GetDeviceFormat();void ResetDeviceFormat();void SetDeviceFormat();void GetProcessingPeriod();void SetProcessingPeriod();void GetShareMode();void SetShareMode();void GetPropertyValue();void SetPropertyValue();
  [PreserveSig]int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)]string id,int role);
  [PreserveSig]int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)]string id,int visible);
 }
 static bool Allowed {get{
#if TESTING && !AUDIO_POLICY_TEST
 return false;
#else
 return !Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--render")||a=="--preview-desk"||a=="--self-test");
#endif
 }}
 static bool cableEnabled;static readonly object gate=new object();
 static bool Cable(MMDevice d){return d.FriendlyName.IndexOf("VB-Audio",StringComparison.OrdinalIgnoreCase)>=0&&d.FriendlyName.IndexOf("CABLE",StringComparison.OrdinalIgnoreCase)>=0;}
 static void Policy(Action<IPolicy> action){var policy=(IPolicy)new PolicyClient();try{action(policy);}finally{Marshal.ReleaseComObject(policy);}}
 public static void RecoverAndHide(){if(!Allowed)return;RestoreAndHide();}
 public static void EnableCable(){if(!Allowed||cableEnabled)return;lock(gate){Guard();using(var devices=new MMDeviceEnumerator())Policy(p=>{foreach(var d in devices.EnumerateAudioEndPoints(DataFlow.All,DeviceState.All))using(d)if(Cable(d))Marshal.ThrowExceptionForHR(p.SetEndpointVisibility(d.ID,1));});cableEnabled=true;}}
 static bool guardStarted;
 static void Guard(){Wire.SaveSecret("audio-owner.bin",System.Diagnostics.Process.GetCurrentProcess().Id.ToString());if(guardStarted)return;var info=new System.Diagnostics.ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath,"--audio-guard "+System.Diagnostics.Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden};System.Diagnostics.Process.Start(info);guardStarted=true;}
 public static void UseCableMicrophone(){UseCable(DataFlow.Capture);}
 public static void UseCableSpeaker(){UseCable(DataFlow.Render);}
 static void UseCable(DataFlow flow){if(!Allowed)return;lock(gate){Guard();using(var devices=new MMDeviceEnumerator()){var cable=devices.EnumerateAudioEndPoints(flow,DeviceState.Active).FirstOrDefault(Cable);if(cable==null)throw new InvalidOperationException("Set up the bundled VB-CABLE device to redirect audio.");using(cable)Policy(p=>{for(int role=0;role<3;role++){string key="audio-"+flow+"-default-"+role+".bin";using(var current=devices.GetDefaultAudioEndpoint(flow,(Role)role)){if(current.ID==cable.ID)continue;if(Wire.LoadSecret(key,"")=="")Wire.SaveSecret(key,current.ID);Marshal.ThrowExceptionForHR(p.SetDefaultEndpoint(cable.ID,role));}}});}}}
 public static void SetHardware(bool input,string name){if(!Allowed)return;using(var devices=new MMDeviceEnumerator()){var flow=input?DataFlow.Capture:DataFlow.Render;var selected=devices.EnumerateAudioEndPoints(flow,DeviceState.Active).FirstOrDefault(d=>!Cable(d)&&(d.FriendlyName==name||name.EndsWith(d.FriendlyName)));if(selected==null)return;using(selected)Policy(p=>{for(int role=0;role<3;role++)Marshal.ThrowExceptionForHR(p.SetDefaultEndpoint(selected.ID,role));});}}
 public static void RestoreDefault(){RestoreFlow(DataFlow.Capture);RestoreFlow(DataFlow.Render);}
 public static void RestoreMicrophone(){RestoreFlow(DataFlow.Capture);}
 public static void RestoreSpeaker(){RestoreFlow(DataFlow.Render);}
 public static string LocalDefaultName(bool input){try{using(var devices=new MMDeviceEnumerator()){var flow=input?DataFlow.Capture:DataFlow.Render;using(var current=devices.GetDefaultAudioEndpoint(flow,Role.Multimedia)){if(!Cable(current))return current.FriendlyName;}string id=Wire.LoadSecret("audio-"+flow+"-default-1.bin","");if(id!="")using(var saved=devices.GetDevice(id))if(saved.State==DeviceState.Active&&!Cable(saved))return saved.FriendlyName;foreach(var candidate in devices.EnumerateAudioEndPoints(flow,DeviceState.Active))using(candidate)if(!Cable(candidate))return candidate.FriendlyName;}}catch{}return "";}
 // Save before muting. A separate guard also restores this journal after a
 // crash, including when no virtual microphone endpoint was needed.
 static Dictionary<string,bool> ReadMutes(){try{return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,bool>>(Wire.LoadSecret("audio-render-mutes.bin","{}"));}catch{return new Dictionary<string,bool>();}}
 public static void SuppressLocalSpeakers(){if(!Allowed)return;lock(gate){Guard();var saved=ReadMutes();using(var devices=new MMDeviceEnumerator())foreach(var device in devices.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active))using(device){if(Cable(device))continue;if(!saved.ContainsKey(device.ID)){saved[device.ID]=device.AudioEndpointVolume.Mute;Wire.SaveSecret("audio-render-mutes.bin",Wire.Json(saved));}device.AudioEndpointVolume.Mute=true;}}}
 public static void RestoreLocalSpeakers(){if(!Allowed)return;lock(gate){var saved=ReadMutes();if(saved.Count==0)return;using(var devices=new MMDeviceEnumerator())foreach(var id in saved.Keys.ToArray()){try{using(var device=devices.GetDevice(id))device.AudioEndpointVolume.Mute=saved[id];saved.Remove(id);}catch{}}Wire.SaveSecret("audio-render-mutes.bin",Wire.Json(saved));}}
 static void RestoreFlow(DataFlow flow){if(!Allowed)return;lock(gate){try{using(var devices=new MMDeviceEnumerator())Policy(p=>{for(int role=0;role<3;role++){string key="audio-"+flow+"-default-"+role+".bin",saved=Wire.LoadSecret(key,"");if(saved=="")continue;using(var current=devices.GetDefaultAudioEndpoint(flow,(Role)role)){if(Cable(current)){int result=p.SetDefaultEndpoint(saved,role);if(result<0)continue;}Wire.SaveSecret(key,"");}}});}catch{}}}
 public static void RestoreAndHide(){if(!Allowed)return;lock(gate){RestoreLocalSpeakers();HideCable();if(ReadMutes().Count==0)Wire.SaveSecret("audio-owner.bin","");}}
 public static void HideCable(){if(!Allowed)return;lock(gate){RestoreDefault();try{using(var devices=new MMDeviceEnumerator())Policy(p=>{foreach(var flow in new[]{DataFlow.Capture,DataFlow.Render}){var all=devices.EnumerateAudioEndPoints(flow,DeviceState.All).ToArray();try{var physical=all.FirstOrDefault(d=>d.State==DeviceState.Active&&!Cable(d));for(int role=0;role<3;role++){try{using(var current=devices.GetDefaultAudioEndpoint(flow,(Role)role))if(Cable(current)&&physical!=null)Marshal.ThrowExceptionForHR(p.SetDefaultEndpoint(physical.ID,role));}catch{}}foreach(var d in all)if(Cable(d))Marshal.ThrowExceptionForHR(p.SetEndpointVisibility(d.ID,0));}finally{foreach(var d in all)d.Dispose();}}});cableEnabled=false;}catch{}}}
}
}

