using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace Velixa {
// One persistent drop target per local monitor, independent of the main window.
public sealed class TransferCorners:IDisposable {
 readonly Func<Device[]> peers;readonly Action<string,string[]> send;readonly Timer timer=new Timer{Interval=1000};
 readonly Dictionary<string,TransferPopup> panels=new Dictionary<string,TransferPopup>();
 public TransferCorners(Func<Device[]> peers,Action<string,string[]> send){this.peers=peers;this.send=send;Sync();timer.Tick+=(s,e)=>Sync();timer.Start();}
 void Sync(){var screens=Screen.AllScreens;foreach(var key in panels.Keys.Where(k=>!screens.Any(s=>s.DeviceName==k)).ToArray()){panels[key].Dispose();panels.Remove(key);}foreach(var screen in screens){TransferPopup panel;if(!panels.TryGetValue(screen.DeviceName,out panel)){panel=new TransferPopup();panel.ConfigureDrop(peers,send);panels.Add(screen.DeviceName,panel);}panel.Place(screen.WorkingArea);if(!panel.Visible)panel.Show();}}
 public void UpdateTransfer(TransferStatus status){foreach(var panel in panels.Values)panel.UpdateTransfer(status);}
 public void Dispose(){timer.Dispose();foreach(var panel in panels.Values)panel.Dispose();panels.Clear();}
}
}
