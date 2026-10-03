using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
namespace Velixa {
public sealed class SavedDesk {
 public string id,name,host,token,fingerprint,devices,pending;
}
// Pairing credentials and each coordinator's cached layout stay DPAPI encrypted.
public static class SavedDesks {
 public static List<SavedDesk> All(){try{return new JavaScriptSerializer().Deserialize<List<SavedDesk>>(Wire.LoadSecret("saved-connections.bin","[]"))??new List<SavedDesk>();}catch{return new List<SavedDesk>();}}
 public static void Remember(){
  string id=Wire.LoadSecret("remote-id-v2.bin",""),token=Wire.LoadSecret("remote-token-v2.bin","");if(id==""||token=="")return;
  var all=All();var saved=all.Find(d=>d.id==id);if(saved==null){saved=new SavedDesk{id=id};all.Add(saved);}
  saved.host=Wire.LoadSecret("host.bin","");saved.token=token;saved.fingerprint=Wire.LoadSecret("remote-fp-v2.bin","");saved.devices=Wire.LoadSecret("client-devices.bin","[]");saved.pending=Wire.LoadSecret("pending-removals.bin","[]");
  try{var host=new JavaScriptSerializer().Deserialize<List<Device>>(saved.devices).Find(d=>d.id==id);if(host!=null)saved.name=host.name;}catch{}
  if(String.IsNullOrWhiteSpace(saved.name))saved.name=saved.host;
  Wire.SaveSecret("saved-connections.bin",Wire.Json(all));
 }
 public static void ClearActive(){foreach(var key in new[]{"remote-token-v2.bin","remote-fp-v2.bin","remote-id-v2.bin","host.bin"})Wire.SaveSecret(key,"");Wire.SaveSecret("client-devices.bin","[]");Wire.SaveSecret("pending-removals.bin","[]");}
 public static SavedDesk Activate(string id){var saved=All().FirstOrDefault(d=>d.id==id);if(saved==null)throw new InvalidOperationException("This desk has not been paired yet.");ClearActive();Wire.SaveSecret("remote-token-v2.bin",saved.token);Wire.SaveSecret("remote-fp-v2.bin",saved.fingerprint);Wire.SaveSecret("remote-id-v2.bin",saved.id);Wire.SaveSecret("host.bin",saved.host);Wire.SaveSecret("client-devices.bin",saved.devices??"[]");Wire.SaveSecret("pending-removals.bin",saved.pending??"[]");return saved;}
 public static void ForgetActive(){string id=Wire.LoadSecret("remote-id-v2.bin","");Wire.SaveSecret("saved-connections.bin",Wire.Json(All().Where(d=>d.id!=id).ToArray()));ClearActive();}
}
}
