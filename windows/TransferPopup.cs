using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace Velixa {
public sealed class TransferPopup:Form {
 TransferStatus current;readonly Timer refresh=new Timer{Interval=120};readonly SoftButton cancel;DateTime finished;
 public TransferPopup(){AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ClientSize=new Size(370,166);BackColor=MainForm.PanelColor;ShowInTaskbar=false;TopMost=true;DoubleBuffered=true;cancel=MainForm.Button("Cancel",()=>{if(current!=null&&!current.Finished)current.Cancel.Cancel();else Hide();});cancel.SetBounds(274,110,76,34);Controls.Add(cancel);refresh.Tick+=(s,e)=>{if(current!=null){cancel.Text=current.Finished?"Close":"Cancel";Invalidate();if(current.Finished){if(finished==DateTime.MinValue)finished=DateTime.UtcNow;if((DateTime.UtcNow-finished).TotalSeconds>8)Hide();}}};refresh.Start();}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
 public void UpdateTransfer(TransferStatus value){if(value.Name.StartsWith("Clipboard")&&!value.Failed)return;if(current!=value)finished=DateTime.MinValue;current=value;var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-20,area.Bottom-Height-20);if(!Visible)Show();Invalidate();}
 static string SizeText(double bytes){return bytes>=1073741824?(bytes/1073741824).ToString("0.0")+" GB":bytes>=1048576?(bytes/1048576).ToString("0.0")+" MB":(bytes/1024).ToString("0.0")+" KB";}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Visual.Stroke(g,new RectangleF(1,1,Width-3,Height-3),16,Color.FromArgb(83,74,116));if(current==null)return;Visual.Text(g,current.Name,new RectangleF(20,14,330,26),15,Color.White,true);Visual.Text(g,current.Message,new RectangleF(20,43,330,22),12,current.Failed?Color.Salmon:MainForm.Muted);var track=new RectangleF(20,82,330,5);Visual.Fill(g,track,2,Color.FromArgb(51,54,69));float fraction=current.Total==0?(current.Finished?1:0):(float)Math.Min(1,current.Done/(double)current.Total);if(fraction>0)Visual.Fill(g,new RectangleF(20,82,330*fraction,5),2,current.Failed?Color.Salmon:MainForm.Accent);string details=(fraction*100).ToString("0")+"% · "+SizeText(current.Done)+" / "+SizeText(current.Total);Visual.Text(g,details,new RectangleF(20,103,244,22),12,Color.White);if(!current.Finished)Visual.Text(g,SizeText(current.Done/Math.Max(.1,current.Clock.Elapsed.TotalSeconds))+"/s",new RectangleF(20,126,235,20),11,MainForm.Muted);}
 protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
}
