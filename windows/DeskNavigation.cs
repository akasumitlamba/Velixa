using System;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;
namespace Velixa {
public partial class MainForm {
 void LeaveCurrentDesk(){Stop();SavedDesks.Remember();SavedDesks.ClearActive();Wire.SaveSecret("role.bin","");paused=false;Welcome();}
 void SwitchSavedDesk(string id){Stop();SavedDesks.Remember();var target=SavedDesks.Activate(id);paused=false;StartReceiver(target.host,"");}
 void SwitchLocalDesk(string id){Stop();SavedDesks.Remember();SavedDesks.ClearActive();paused=false;if(id!=""){var store=ReadLocalDesks();if(store.desks.Any(d=>d.id==id)){store.active=id;Wire.SaveSecret("desks-v3.bin",Wire.Json(store));}}StartHost();}
 DeskStore ReadLocalDesks(){try{return new JavaScriptSerializer().Deserialize<DeskStore>(Wire.LoadSecret("desks-v3.bin","{}"))??new DeskStore();}catch{return new DeskStore();}}
 void CreateOwnDesk(){if(ReadLocalDesks().desks.Count>=12){Info("Desk limit","You can keep up to 12 desks on this PC. Delete an unused local desk first.");return;}NameDesk("create","New desk");}
 void BrowseDesks(){LeaveCurrentDesk();JoinView();}
 void DesksMenu(){
  if(network==null||!network.Connecting)SavedDesks.Remember();var menu=DarkMenu.Create();
  menu.Items.Add("Create my own desk…",null,(s,e)=>CreateOwnDesk());
  menu.Items.Add("Find nearby desks…",null,(s,e)=>BrowseDesks());
  menu.Items.Add("Leave current desk",null,(s,e)=>LeaveCurrentDesk()).Enabled=session!=null;
  var local=ReadLocalDesks();if(local.desks.Count>0){menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Desks on this PC").Enabled=false;foreach(var profile in local.desks){string id=profile.id;var item=new ToolStripMenuItem(profile.name){Checked=network!=null&&network.IsHost&&session!=null&&session.Store.active==id};item.Click+=(s,e)=>SwitchLocalDesk(id);menu.Items.Add(item);}}
  var saved=SavedDesks.All();if(saved.Count>0){menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Paired desks · reconnect without a code").Enabled=false;foreach(var target in saved){string id=target.id;var item=new ToolStripMenuItem(target.name){Checked=network!=null&&!network.IsHost&&Wire.LoadSecret("remote-id-v2.bin","")==id};item.Click+=(s,e)=>SwitchSavedDesk(id);menu.Items.Add(item);}}
  if(network!=null&&network.IsHost&&session!=null){menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Rename current desk…",null,(s,e)=>NameDesk("rename",session.DeskName));menu.Items.Add("Delete current desk…",null,(s,e)=>{if(ConfirmAction("Delete saved desk?","Delete "+session.DeskName+"? Paired devices are retained.","Delete")){session.Profile("delete","");DeskView();}}).Enabled=session.Store.desks.Count>1;}
  menu.Closed+=(s,e)=>menu.Dispose();menu.Show(deskButton,0,deskButton.Height);
 }
}
}
