using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace Velixa {
public sealed class TransferPopup:Form {
 TransferStatus current;readonly List<TransferStatus> jobs=new List<TransferStatus>();readonly Timer refresh=new Timer{Interval=120};readonly SoftButton cancel;DateTime finished;
 Func<Device[]> peers;Action<string,string[]> send;bool hovering;string notice="Drop files here to send";
 public TransferPopup(){AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ClientSize=new Size(370,166);BackColor=MainForm.PanelColor;ShowInTaskbar=false;TopMost=true;DoubleBuffered=true;AllowDrop=true;
 cancel=MainForm.Button("Files",()=>{if(current!=null&&!current.Finished)current.Cancel.Cancel();else if(current!=null){jobs.Remove(current);current=null;finished=DateTime.MinValue;Invalidate();}else{using(var picker=new OpenFileDialog{Multiselect=true,Title="Send files to a connected PC"})if(picker.ShowDialog()==DialogResult.OK)AcceptFiles(picker.FileNames);}});cancel.SetBounds(274,110,76,34);Controls.Add(cancel);
 DragEnter+=(s,e)=>{hovering=e.Data!=null&&e.Data.GetDataPresent(DataFormats.FileDrop)&&peers!=null&&peers().Length>0;e.Effect=hovering?DragDropEffects.Copy:DragDropEffects.None;Invalidate();};
 DragOver+=(s,e)=>e.Effect=e.Data!=null&&e.Data.GetDataPresent(DataFormats.FileDrop)&&peers!=null&&peers().Length>0?DragDropEffects.Copy:DragDropEffects.None;
 DragLeave+=(s,e)=>{hovering=false;Invalidate();};
 DragDrop+=(s,e)=>{hovering=false;if(e.Data!=null&&e.Data.GetDataPresent(DataFormats.FileDrop))AcceptFiles((string[])e.Data.GetData(DataFormats.FileDrop));Invalidate();};
 refresh.Tick+=(s,e)=>{if(current==null||current.Finished){var active=jobs.FirstOrDefault(j=>!j.Finished);if(active!=null){current=active;finished=DateTime.MinValue;}}cancel.Text=current==null?"Files":current.Finished?"Clear":"Cancel";if(current!=null&&current.Finished){if(finished==DateTime.MinValue)finished=DateTime.UtcNow;if(!current.Failed&&(DateTime.UtcNow-finished).TotalSeconds>8){jobs.RemoveAll(j=>j.Finished);current=null;finished=DateTime.MinValue;}}Invalidate();};refresh.Start();Place(Screen.PrimaryScreen.WorkingArea);}
 public void ConfigureDrop(Func<Device[]> destinations,Action<string,string[]> transfer){peers=destinations;send=transfer;}
 public void Place(Rectangle area){Location=new Point(area.Right-Width-20,area.Bottom-Height-20);}
 public void AcceptFiles(string[] paths){if(paths==null||paths.Length==0||peers==null||send==null)return;var available=peers();if(available.Length==0){notice="No connected Windows PC";Invalidate();return;}if(available.Length==1){SendTo(available[0].id,paths);return;}var menu=DarkMenu.Create();foreach(var peer in available){string id=peer.id;menu.Items.Add("Send to "+peer.name,null,(s,e)=>SendTo(id,paths));}menu.Closed+=(s,e)=>menu.Dispose();menu.Show(this,new Point(0,0));}
 void SendTo(string id,string[] paths){if(!peers().Any(p=>p.id==id)){notice="Device disconnected. Drop again to retry.";Invalidate();return;}notice="Preparing transfer…";send(id,paths);Invalidate();}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);try{foreach(uint message in new uint[]{0x233,0x4a,0x49})MainForm.ChangeWindowMessageFilterEx(Handle,message,1,IntPtr.Zero);MainForm.DragAcceptFiles(Handle,true);}catch{}}
 protected override void WndProc(ref Message m){if(m.Msg==0x233){try{uint count=MainForm.DragQueryFile(m.WParam,0xffffffff,null,0);var paths=new List<string>();for(uint i=0;i<count;i++){uint length=MainForm.DragQueryFile(m.WParam,i,null,0);var path=new System.Text.StringBuilder((int)length+1);if(MainForm.DragQueryFile(m.WParam,i,path,(uint)path.Capacity)>0)paths.Add(path.ToString());}AcceptFiles(paths.ToArray());}finally{MainForm.DragFinish(m.WParam);}m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
 public void UpdateTransfer(TransferStatus value){if(value.IsClipboard&&!value.Failed)return;bool added=!jobs.Contains(value);if(added){jobs.Add(value);if(current==null||current.Finished){current=value;finished=DateTime.MinValue;}}if(!Visible)Show();Invalidate();}
 static string SizeText(double bytes){return bytes>=1073741824?(bytes/1073741824).ToString("0.0")+" GB":bytes>=1048576?(bytes/1048576).ToString("0.0")+" MB":(bytes/1024).ToString("0.0")+" KB";}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Visual.Stroke(g,new RectangleF(1,1,Width-3,Height-3),16,hovering?MainForm.Accent:Color.FromArgb(83,74,116));if(current==null){var targets=peers==null?new Device[0]:peers();Visual.Text(g,"Velixa · File transfer",new RectangleF(20,14,330,26),15,Color.White,true);Visual.Text(g,hovering?"Release to send files":notice,new RectangleF(20,48,330,26),13,Color.White);Visual.Text(g,targets.Length==1?"To "+targets[0].name:targets.Length==0?"Waiting for a connected Windows PC":"Choose a destination after dropping",new RectangleF(20,80,330,24),12,MainForm.Muted);Visual.Text(g,"Saved in Downloads / Velixa",new RectangleF(20,122,244,22),11,MainForm.Muted);return;}
 Visual.Text(g,current.Name,new RectangleF(20,14,330,26),15,Color.White,true);Visual.Text(g,current.Message,new RectangleF(20,43,330,22),12,current.Failed?Color.Salmon:MainForm.Muted);var track=new RectangleF(20,82,330,5);Visual.Fill(g,track,2,Color.FromArgb(51,54,69));float fraction=current.Total==0?(current.Finished?1:0):(float)Math.Min(1,current.Done/(double)current.Total);if(fraction>0)Visual.Fill(g,new RectangleF(20,82,330*fraction,5),2,current.Failed?Color.Salmon:MainForm.Accent);string details=(fraction*100).ToString("0")+"% · "+SizeText(current.Done)+" / "+SizeText(current.Total);Visual.Text(g,details,new RectangleF(20,103,244,22),12,Color.White);int remaining=jobs.Count(j=>!j.Finished);if(!current.Finished)Visual.Text(g,SizeText(current.Done/Math.Max(.1,current.Clock.Elapsed.TotalSeconds))+"/s"+(remaining>1?" · "+remaining+" active":""),new RectangleF(20,126,235,20),11,MainForm.Muted);}
 protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
}
