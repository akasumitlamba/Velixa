using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Velixa {
public partial class MainForm {
 SoftButton retryButton,disconnectButton;Label connectionLabel,selectedName,deviceBadge; ToggleSwitch inputToggle,clipboardToggle;
 void BuildChrome(){
  var brand=new Panel{Dock=DockStyle.Top,Height=78};Controls.Add(brand);
  brand.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,"logo",new RectangleF(23,18,38,38),Accent);using(var p=new Pen(Color.FromArgb(28,37,51)))e.Graphics.DrawLine(p,0,77,brand.Width,77);};
  LabelAt(brand,"Velixa",78,12,180,32,26,Color.White,true);LabelAt(brand,"One input. Everywhere.",79,46,220,22,13,Muted);
  var settings=Button("",()=>{Settings();RefreshInspector();});settings.Name="LegacyHeaderSettings";settings.Width=40;settings.IconName="settings";settings.Paint+=(s,e)=>UiIcons.Draw(e.Graphics,"settings",new RectangleF(10,10,20,20),Muted);settings.Visible=false;brand.Controls.Add(settings);
  var more=Button("",()=>{var menu=DarkMenu.Create();menu.Items.Add(transfers!=null&&transfers.Visible?"Hide file transfer panel":"Show file transfer panel",null,(s,e)=>{if(transfers!=null)transfers.SetVisible(!transfers.Visible);}).Enabled=transfers!=null;menu.Items.Add("Open received files",null,(s,e)=>OpenReceivedFiles());menu.Items.Add("Hide to tray",null,(s,e)=>Hide());menu.Items.Add("Quit Velixa",null,(s,e)=>QuitApp());menu.Show(brand,new Point(brand.Width-200,64));});more.IconName="more";more.Paint+=(s,e)=>UiIcons.Draw(e.Graphics,"more",new RectangleF(10,10,20,20),Muted);brand.Controls.Add(more);
  var badge=new SoftPanel{Radius=20,FillColor=PanelColor};brand.Controls.Add(badge);badge.Paint+=(s,e)=>{using(var b=new SolidBrush(session!=null&&session.Connected?Color.FromArgb(57,202,159):Muted))e.Graphics.FillEllipse(b,14,14,12,12);};connectionLabel=LabelAt(badge,"Not connected",34,11,106,22,12,Color.White);
  Action place=()=>{more.SetBounds(brand.Width-56,20,40,40);settings.SetBounds(brand.Width-110,20,40,40);badge.SetBounds(brand.Width-268,20,142,40);};brand.Resize+=(s,e)=>place();place();
  var footer=new Panel{Dock=DockStyle.Bottom,Height=40};Controls.Add(footer);footer.Paint+=(s,e)=>{using(var p=new Pen(Color.FromArgb(28,37,51)))e.Graphics.DrawLine(p,0,0,footer.Width,0);using(var b=new SolidBrush(session!=null&&session.Connected?Color.FromArgb(57,202,159):Muted))e.Graphics.FillEllipse(b,20,16,12,12);};
  status=LabelAt(footer,"Private by design. Connected on your network.",42,12,1000,22,12,Muted);status.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top;
 }
 void ToggleInput(){if(session==null)return;paused=!paused;session.Paused=paused;if(paused){session.Input.Home();receiver.Handle(Wire.Parse("{\"t\":\"leave\"}"));}if(!session.Preview)session.SleepDevice(Network.LocalId,paused);session.Input.Enabled=!paused&&session.Connected&&(session.Automatic||session.Source==Network.LocalId);UpdateDesk();}
 void Info(string title,string text){using(var f=new Form{Text=title,ClientSize=new Size(450,210),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){Visual.DarkTitle(f);LabelAt(f,title,24,20,400,35,24,Color.White,true);LabelAt(f,text,24,69,400,80,14,Muted);var done=Button("Done",()=>f.Close(),true);done.SetBounds(300,156,126,36);f.Controls.Add(done);f.ShowDialog(this);}}
 ToggleSwitch mediaToggle,filesToggle;SoftButton mediaMic,mediaSpeaker,localMic,localSpeaker,sleepSelected,removeSelected,sizeSelected;Label selectedDetails,mediaState;Panel sharingPanel,devicePanel,localPanel;
 void BuildDesk(){
  activeSection="My Desk";Clear();var root=new Panel{Dock=DockStyle.Fill};content.Controls.Add(root);
  var center=new SoftPanel{FillColor=Color.FromArgb(17,23,34)};var sidebar=new Panel{BackColor=Bg};var right=new Panel{BackColor=Bg,AutoScroll=true};sidebar.Controls.Add(right);root.Controls.AddRange(new Control[]{center,sidebar});
  var title=LabelAt(center,session.DeskName,24,22,350,40,28,Color.White,true);
  var add=new DeskActionButton{Text="Add device",Primary=true,Name="AddDevice"};add.Click+=(s,e)=>AddDeskDevice();center.Controls.Add(add);
  deskButton=new DeskActionButton{Text="Switch desk"};deskButton.Click+=(s,e)=>DesksMenu();center.Controls.Add(deskButton);var leave=Button("Leave desk",LeaveCurrentDesk);leave.Name="LeaveDesk";center.Controls.Add(leave);
  desk=new DeskCanvas(session){Name="Arrangement",FilesDropped=(id,paths)=>{if(sharing!=null)sharing.SendFiles(id,paths);}};desk.Selected=session.Devices.FirstOrDefault(d=>d.id==Network.LocalId);desk.SelectionChanged=RefreshInspector;center.Controls.Add(desk);
  var screen=new SoftPanel{FillColor=PanelColor,Name="ScreenLayout"};desk.Controls.Add(screen);var zoomOut=Button("−",()=>desk.Zoom(-.2f));zoomOut.AccessibleName="Zoom out";zoomOut.SetBounds(8,6,38,34);var zoomIn=Button("+",()=>desk.Zoom(.2f));zoomIn.AccessibleName="Zoom in";zoomIn.SetBounds(52,6,38,34);var fit=Button("Fit",()=>desk.Fit());fit.SetBounds(96,6,60,34);screen.Controls.AddRange(new Control[]{zoomOut,zoomIn,fit});
  sharingPanel=InspectorPanel(right,"This PC · sharing","bluetooth");devicePanel=InspectorPanel(right,"Selected device","monitor");localPanel=InspectorPanel(right,"This PC · hardware","settings");
  LabelAt(sharingPanel,"Media sharing",18,44,210,24,13,Color.White);LabelAt(sharingPanel,"Clipboard sharing",18,72,210,24,13,Color.White);LabelAt(sharingPanel,"File sharing",18,100,210,24,13,Color.White);
  mediaToggle=new ToggleSwitch{AccessibleName="Media sharing"};mediaToggle.Click+=(s,e)=>SetMediaEnabled(!mediaEnabled);sharingPanel.Controls.Add(mediaToggle);
  clipboardToggle=new ToggleSwitch{AccessibleName="Clipboard sharing"};clipboardToggle.Click+=(s,e)=>{if(sharing==null)return;sharing.ClipboardEnabled=!sharing.ClipboardEnabled;Wire.SaveSecret("clipboard-enabled.bin",sharing.ClipboardEnabled?"true":"false");RefreshInspector();};sharingPanel.Controls.Add(clipboardToggle);
  filesToggle=new ToggleSwitch{AccessibleName="File sharing"};filesToggle.Click+=(s,e)=>{if(sharing==null)return;sharing.FilesEnabled=!sharing.FilesEnabled;Wire.SaveSecret("files-enabled.bin",sharing.FilesEnabled?"true":"false");RefreshInspector();};sharingPanel.Controls.Add(filesToggle);
  mediaMic=Button("Microphone source",()=>MediaSourceMenu(true));mediaMic.AccessibleName="Microphone source";sharingPanel.Controls.Add(mediaMic);mediaSpeaker=Button("Speaker destination",()=>MediaSourceMenu(false));mediaSpeaker.AccessibleName="Speaker destination";sharingPanel.Controls.Add(mediaSpeaker);
  mediaState=LabelAt(sharingPanel,"",18,258,250,34,11,Muted);
  selectedName=LabelAt(devicePanel,"",18,60,250,28,18,Color.White,true);selectedName.AutoEllipsis=true;selectedDetails=LabelAt(devicePanel,"",18,96,250,50,13,Muted);
  sizeSelected=Button("Adjust screen size",()=>desk.EditSize(desk.Selected));sizeSelected.Name="DeviceActions";devicePanel.Controls.Add(sizeSelected);
  sleepSelected=Button("Disconnect for now",()=>{if(desk.Selected!=null)session.SleepDevice(desk.Selected.id,!desk.Selected.sleeping);});devicePanel.Controls.Add(sleepSelected);
  removeSelected=Button("Remove from this desk",()=>{if(desk.Selected!=null)session.RemoveDevice(desk.Selected.id);});devicePanel.Controls.Add(removeSelected);
  retryButton=Button("Reconnect now",RetryDesk);devicePanel.Controls.Add(retryButton);
  sourceButton=Button("Automatic",SourceMenu);localPanel.Controls.Add(sourceButton);sourceName=LabelAt(localPanel,"",18,126,250,22,11,Muted);
  localMic=Button("My microphone",()=>LocalMediaMenu(true));localPanel.Controls.Add(localMic);localSpeaker=Button("My speaker",()=>LocalMediaMenu(false));localPanel.Controls.Add(localSpeaker);
  micLabel=LabelAt(localPanel,"",18,246,260,38,11,Muted);micLabel.AutoEllipsis=true;
  var quit=Button("Quit app",QuitApp);quit.Name="QuitApp";quit.Height=40;sidebar.Controls.Add(quit);right.BringToFront();
  // Legacy helpers retain these references, but no navigation or pause/disconnect controls are displayed.
  inputToggle=new ToggleSwitch();pauseButton=new SoftButton();disconnectButton=new SoftButton();deviceBadge=new Label();deskCount=new Label();micButton=localMic;
  Action layout=()=>{int rightW=root.Width<1100?300:336;sidebar.SetBounds(root.Width-rightW,0,rightW,root.Height);center.SetBounds(0,0,root.Width-rightW-16,root.Height);quit.SetBounds(0,root.Height-40,rightW,40);right.SetBounds(0,0,rightW,Math.Max(0,root.Height-48));int w=rightW-SystemInformation.VerticalScrollBarWidth-4;
   title.SetBounds(24,18,center.Width-48,38);add.SetBounds(center.Width-432,67,128,40);deskButton.SetBounds(center.Width-292,67,150,40);leave.SetBounds(center.Width-130,67,106,40);desk.SetBounds(16,122,center.Width-32,Math.Max(170,center.Height-138));screen.SetBounds(desk.Width-182,desk.Height-58,166,46);screen.BringToFront();
   var scroll=right.AutoScrollPosition;int top=mediaEnabled?210:130;sharingPanel.SetBounds(0,8+scroll.Y,w,top);mediaToggle.SetBounds(w-58,44,40,24);clipboardToggle.SetBounds(w-58,72,40,24);filesToggle.SetBounds(w-58,100,40,24);mediaMic.SetBounds(18,132,w-36,32);mediaSpeaker.SetBounds(18,170,w-36,32);mediaState.Visible=false;mediaState.Width=w-36;
   int mid=Math.Max(network!=null&&!network.IsHost&&!session.Connected?258:226,right.Height-top-174-32);devicePanel.SetBounds(0,top+20+scroll.Y,w,mid);selectedName.Width=selectedDetails.Width=w-36;selectedName.Top=54;selectedDetails.Top=82;selectedDetails.Height=32;sizeSelected.SetBounds(18,114,w-36,32);sleepSelected.SetBounds(18,148,w-36,32);removeSelected.SetBounds(18,186,w-36,32);retryButton.SetBounds(18,224,w-36,28);
   int bottom=top+mid+32;localPanel.SetBounds(0,bottom+scroll.Y,w,174);sourceButton.SetBounds(18,48,w-36,36);sourceName.Visible=false;localMic.SetBounds(18,90,w-36,36);localSpeaker.SetBounds(18,132,w-36,36);micLabel.Visible=false;right.AutoScrollMinSize=new Size(0,bottom+174);
  };root.Resize+=(s,e)=>layout();sharingPanel.Tag=layout;layout();sectionHost=root;deskCenter=center;deskInspector=right;navItems=new NavigationButton[0];sectionPage=new ScrollSurface{Visible=false};UpdateDesk();
 }
 void AddDeskDevice(){var menu=DarkMenu.Create();var known=session.Store.desks.SelectMany(p=>p.devices).Where(d=>!session.Devices.Any(v=>v.id==d.id)).GroupBy(d=>d.id).Select(g=>g.First()).ToArray();foreach(var d in known){var id=d.id;menu.Items.Add("Add "+d.name,null,(s,e)=>session.AddSavedDevice(id));}if(network!=null&&network.IsHost)menu.Items.Add("Pair a new device…",null,(s,e)=>PairDialog());else menu.Items.Add("Pair new devices on the desk's main PC").Enabled=false;menu.Show(deskCenter,new Point(Math.Max(0,deskCenter.Width-275),110));}
 Label micLabel;
 SoftPanel InspectorPanel(Control parent,string title,string icon){var p=new SoftPanel{FillColor=PanelColor};parent.Controls.Add(p);p.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,icon,new RectangleF(18,20,22,22),Color.FromArgb(91,145,255));Visual.Text(e.Graphics,title,new RectangleF(55,15,p.Width-65,32),15,Color.White,true);};return p;}
 void IconLabel(Control p,string text,string icon,int y){p.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,icon,new RectangleF(18,y+3,19,19),Muted);Visual.Text(e.Graphics,text,new RectangleF(47,y,p.Width-106,27),p.Width<250?11:13,Color.FromArgb(228,234,245));};}
 void RefreshInspector(){if(session==null||selectedName==null||selectedName.IsDisposed)return;var selected=desk.Selected;if(selected!=null&&!session.Devices.Contains(selected))selected=desk.Selected=session.Devices.Find(d=>d.id==selected.id);selectedName.Text=selected==null?"Select a device":selected.name+(selected.id==Network.LocalId?" · This PC":"");selectedDetails.Text=selected==null?"Click a screen on your desk.":selected.kind+" · "+(selected.sleeping?"Disconnected for now":selected.Available?"Connected":"Offline — reconnects automatically")+"\nScreen size: "+Math.Round(selected.scale*100)+"%";sizeSelected.Enabled=sleepSelected.Enabled=selected!=null&&session.Connected;sleepSelected.Text=selected!=null&&selected.sleeping?"Wake / reconnect":"Disconnect for now";removeSelected.Enabled=selected!=null&&selected.id!=Network.LocalId&&session.Connected;sourceButton.Enabled=session.Connected;deskButton.Enabled=true;sourceButton.Text=session.Automatic?"Mouse / keyboard: Automatic":"Mouse / keyboard source";clipboardToggle.Enabled=filesToggle.Enabled=sharing!=null;clipboardToggle.Checked=sharing!=null&&sharing.ClipboardEnabled;filesToggle.Checked=sharing!=null&&sharing.FilesEnabled;clipboardToggle.Invalidate();filesToggle.Invalidate();connectionLabel.Text=session.Connected?(session.Devices.Any(d=>d.id!=Network.LocalId&&d.Available)?"Connected":"No peers online"):network!=null&&network.Connecting?"Reconnecting":"Offline";retryButton.Visible=network!=null&&!network.IsHost&&!session.Connected;retryButton.Enabled=true;connectionLabel.Parent.Invalidate();RefreshMediaControls();}

}
public class DeskActionButton:SoftButton {
 bool hover,pressed;
 protected override void OnMouseEnter(EventArgs e){hover=true;base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){pressed=true;base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){pressed=false;base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;g.Clear(Color.FromArgb(17,23,34));var r=new RectangleF(.5f,.5f,Width-1,Height-1);using(var path=Visual.Round(r,11))using(var fill=new LinearGradientBrush(r,Primary?Color.FromArgb(hover?139:126,107,255):Color.FromArgb(32,41,59),Primary?Color.FromArgb(pressed?91:108,79,245):Color.FromArgb(27,35,51),90)){g.FillPath(fill,path);}Visual.Stroke(g,r,11,Primary?Color.FromArgb(146,124,255):Color.FromArgb(47,59,82));using(var pen=new Pen(Enabled?Color.White:MainForm.Muted,1.6f){StartCap=LineCap.Round,EndCap=LineCap.Round}){if(Primary){g.DrawLine(pen,18,Height/2-5,18,Height/2+5);g.DrawLine(pen,13,Height/2,23,Height/2);}else{float x=Width-24,y=Height/2;g.DrawLines(pen,new[]{new PointF(x-3,y-2),new PointF(x,y+1),new PointF(x+3,y-2)});}}Visual.Text(g,Text,new RectangleF(Primary?32:17,0,Width-(Primary?39:47),Height),14,Enabled?Color.White:MainForm.Muted,true);if(Focused&&ShowFocusCues)Visual.Stroke(g,new RectangleF(3,3,Width-7,Height-7),8,Color.White);}
}
public class ToggleSwitch:Control {
 public bool Checked;public ToggleSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.CheckButton;}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(MainForm.PanelColor);Visual.Fill(g,new RectangleF(0,0,Width-1,Height-1),12,Enabled&&Checked?MainForm.Accent:Color.FromArgb(54,64,81));using(var b=new SolidBrush(Enabled?Color.White:MainForm.Muted))g.FillEllipse(b,Checked?Width-21:3,3,18,18);if(Focused)Visual.Stroke(g,new RectangleF(1,1,Width-3,Height-3),11,Color.White);}
}
public class NavigationButton:Button {
 public bool Selected{get{return selected;}set{selected=value;Invalidate();}}string icon;bool selected,hover;public NavigationButton(string title,string symbol,bool active){Text=title;icon=symbol;selected=active;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;AccessibleName=title;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(15,21,31));if(selected||hover){Visual.Fill(g,new RectangleF(0,0,Width-1,Height-1),9,Color.FromArgb(32,41,62));Visual.Stroke(g,new RectangleF(.5f,.5f,Width-1,Height-1),9,Color.FromArgb(51,63,90));}if(selected)using(var p=new Pen(MainForm.Accent,2))g.DrawLine(p,1,9,1,Height-9);UiIcons.Draw(g,icon,new RectangleF(13,12,20,20),selected?Color.FromArgb(123,201,255):MainForm.Muted);Visual.Text(g,Text,new RectangleF(46,0,Width-51,Height),14,selected?Color.White:MainForm.Muted,selected);}
}
public static class UiIcons {
 public static void Draw(Graphics g,string kind,RectangleF r,Color color){var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;float size=Math.Min(r.Width,r.Height);g.TranslateTransform(r.X+(r.Width-size)/2,r.Y+(r.Height-size)/2);g.ScaleTransform(size/24,size/24);using(var p=new Pen(color,1.7f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})using(var b=new SolidBrush(color)){
  if(kind=="logo") {Logo(g);}
  else if(kind=="windows"){g.FillPolygon(b,new[]{new PointF(1,4),new PointF(10,2.7f),new PointF(10,11),new PointF(1,11)});g.FillPolygon(b,new[]{new PointF(12,2.4f),new PointF(23,1),new PointF(23,11),new PointF(12,11)});g.FillPolygon(b,new[]{new PointF(1,13),new PointF(10,13),new PointF(10,21.3f),new PointF(1,20)});g.FillPolygon(b,new[]{new PointF(12,13),new PointF(23,13),new PointF(23,23),new PointF(12,21.6f)});}
  else if(kind=="android"){g.FillPie(b,5,5,14,12,180,180);g.FillRectangle(b,5,11,14,8);Visual.Fill(g,new RectangleF(1,11,3,8),1.5f,color);Visual.Fill(g,new RectangleF(20,11,3,8),1.5f,color);g.DrawLine(p,8,18,8,23);g.DrawLine(p,16,18,16,23);g.DrawLine(p,7,5,5,2);g.DrawLine(p,17,5,19,2);using(var eye=new SolidBrush(Color.FromArgb(23,38,66))){g.FillEllipse(eye,8,7,2,2);g.FillEllipse(eye,14,7,2,2);}}
  else if(kind=="monitor"){g.DrawRectangle(p,2,3,20,14);g.DrawLine(p,12,17,12,21);g.DrawLine(p,7,21,17,21);}
  else if(kind=="devices"){g.DrawRectangle(p,2,3,16,13);g.DrawLine(p,8,16,8,20);g.DrawLine(p,4,20,11,20);Visual.Fill(g,new RectangleF(14,8,9,14),2,MainForm.PanelColor);g.DrawRectangle(p,14,8,8,13);g.DrawLine(p,17,18,19,18);}
  else if(kind=="settings"){for(int i=0;i<8;i++){double a=i*Math.PI/4;g.DrawLine(p,12+(float)Math.Cos(a)*8,12+(float)Math.Sin(a)*8,12+(float)Math.Cos(a)*10,12+(float)Math.Sin(a)*10);}g.DrawEllipse(p,5,5,14,14);g.DrawEllipse(p,9,9,6,6);}
  else if(kind=="grid"){for(int y=3;y<20;y+=11)for(int x=3;x<20;x+=11)g.DrawRectangle(p,x,y,7,7);}
  else if(kind=="info"){g.DrawEllipse(p,2,2,20,20);g.DrawLine(p,12,11,12,17);g.FillEllipse(b,11,6,2,2);}
  else if(kind=="bluetooth"){g.DrawLines(p,new[]{new PointF(6,6),new PointF(18,17),new PointF(12,22),new PointF(12,2),new PointF(18,7),new PointF(6,18)});}
  else if(kind=="clipboard"){g.DrawRectangle(p,5,5,14,17);Visual.Fill(g,new RectangleF(9,2,6,6),1,MainForm.PanelColor);g.DrawRectangle(p,9,2,6,6);g.DrawLine(p,9,13,15,13);g.DrawLine(p,9,17,15,17);}
  else if(kind=="keyboard"){g.DrawRectangle(p,1,5,22,14);for(int y=9;y<=12;y+=3)for(int x=5;x<21;x+=4)g.DrawLine(p,x,y,x+.4f,y);g.DrawLine(p,7,16,17,16);}
  else if(kind=="cursor"){g.FillPolygon(b,new[]{new PointF(5,2),new PointF(21,14),new PointF(14,15),new PointF(17,21),new PointF(13,23),new PointF(10,16),new PointF(5,21)});}
  else if(kind=="more"){for(int x=5;x<=19;x+=7)g.FillEllipse(b,x-1,11,2,2);}
 }g.Restore(state);}
 static Image brandLogo;
 static void Logo(Graphics g){if(brandLogo==null){string path=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"velixa-logo.png");if(System.IO.File.Exists(path))brandLogo=Image.FromFile(path);}if(brandLogo!=null){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(brandLogo,new RectangleF(0,0,24,24));}}

}
public static class DeskArtwork {
 public static void Paint(Graphics g,Rectangle area,DeskSession session,Device selected,Device drop,Func<Device,RectangleF> card){g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(17,23,34));var canvas=new RectangleF(0.5f,.5f,area.Width-1,area.Height-1);Visual.Fill(g,canvas,12,Color.FromArgb(15,22,32));using(var dot=new SolidBrush(Color.FromArgb(40,52,71)))for(int y=12;y<area.Height;y+=13)for(int x=12;x<area.Width;x+=13)g.FillEllipse(dot,x,y,1.3f,1.3f);Visual.Stroke(g,canvas,12,Color.FromArgb(36,46,63));
  int number=0;foreach(var d in session.Devices){number++;var r=card(d);if(!d.Available&&d.id!=Network.LocalId){Visual.Fill(g,r,3,Color.FromArgb(79,89,108));if(d==selected)Visual.Stroke(g,r,3,MainForm.Accent,2);continue;}r.Inflate(-7,-7);if(r.Width<8||r.Height<8)continue;bool local=d.id==Network.LocalId,phone=d.kind=="Android";Color border=local?Color.FromArgb(255,197,67):Color.FromArgb(113,102,255);var saved=g.Save();using(var clip=Visual.Round(r,12)){g.SetClip(clip);using(var bg=new LinearGradientBrush(r,local?Color.FromArgb(8,27,53):Color.FromArgb(17,30,73),local?Color.FromArgb(35,87,122):Color.FromArgb(71,44,123),65))g.FillRectangle(bg,r);
   // Smooth vector landscape contours stay sharp at every screen size.
   for(int layer=0;layer<4;layer++){float y=r.Top+r.Height*(.58f+layer*.095f);using(var ridge=new GraphicsPath()){ridge.AddBezier(r.Left-10,y,r.Left+r.Width*.2f,y-r.Height*.18f,r.Left+r.Width*.35f,y+r.Height*.32f,r.Left+r.Width*.58f,y+r.Height*.13f);ridge.AddBezier(r.Left+r.Width*.58f,y+r.Height*.13f,r.Left+r.Width*.78f,y,r.Right-r.Width*.12f,y-r.Height*.12f,r.Right+10,y-r.Height*.13f);ridge.AddLine(r.Right+10,y-r.Height*.13f,r.Right+10,r.Bottom+10);ridge.AddLine(r.Right+10,r.Bottom+10,r.Left-10,r.Bottom+10);ridge.CloseFigure();using(var fill=new LinearGradientBrush(r,local?Color.FromArgb(22-layer*4,62-layer*10,97-layer*14):Color.FromArgb(43-layer*7,54-layer*10,126-layer*19),Color.FromArgb(5,15,30),80))g.FillPath(fill,ridge);}}
   if(!d.Available)using(var dim=new SolidBrush(Color.FromArgb(120,13,18,27)))g.FillRectangle(dim,r);
  }g.Restore(saved);if(d==selected||d==drop)Visual.Stroke(g,new RectangleF(r.X-2,r.Y-2,r.Width+4,r.Height+4),14,Color.FromArgb(55,border),3);Visual.Stroke(g,r,12,d.Available?border:MainForm.Muted,2);
   float badge=Math.Min(36,Math.Min(r.Width,r.Height)*.22f);if(badge>=18){Visual.Fill(g,new RectangleF(r.X+10,r.Y+10,badge,badge),9,Color.FromArgb(58,75,111));Visual.Text(g,number.ToString(),new RectangleF(r.X+10,r.Y+10,badge,badge),14,Color.White,true,StringAlignment.Center);}
   bool compactCard=r.Height<145||r.Width<100;float icon=Math.Min(32,Math.Min(r.Width*.4f,r.Height*.24f));
   if(r.Height>30&&r.Width>22){float cy=compactCard?r.Y+r.Height*.56f:r.Y+r.Height*.46f;UiIcons.Draw(g,phone?"android":"windows",new RectangleF(r.X+(r.Width-icon)/2,cy-icon/2,icon,icon),phone?Color.FromArgb(163,218,248):Color.FromArgb(40,185,249));
    if(compactCard)Visual.Text(g,d.name,new RectangleF(r.X-3,r.Bottom+4,r.Width+6,20),11,Color.White,true,StringAlignment.Center);
    else {Visual.Text(g,d.name,new RectangleF(r.X+7,cy+icon/2+10,r.Width-14,23),r.Width<145?12:14,Color.White,true,StringAlignment.Center);Visual.Text(g,!d.Available?(d.sleeping?"Sleeping":"Offline"):local?"This device":phone?"Phone":"Laptop",new RectangleF(r.X+7,cy+icon/2+34,r.Width-14,20),12,Color.FromArgb(188,201,224),false,StringAlignment.Center);}
   }

   if(d.id==session.Source&&r.Width>130&&r.Height>160){Visual.Fill(g,new RectangleF(r.X+10,r.Bottom-35,72,25),10,Color.FromArgb(41,56,88));Visual.Text(g,"Primary",new RectangleF(r.X+10,r.Bottom-35,72,25),11,Color.White,true,StringAlignment.Center);}if(d==selected&&r.Width>100)UiIcons.Draw(g,"more",new RectangleF(r.Right-35,r.Y+14,22,22),Color.White);
  }
  foreach(var d in session.Devices.Where(v=>!v.Available&&v.id!=Network.LocalId)){var r=card(d);bool vertical=r.Height>r.Width;float width=Math.Min(170,area.Width-8);float x=Math.Max(4,Math.Min(area.Width-width-4,vertical?r.Left+r.Width/2-width/2:r.Left+(r.Width-width)/2));float y=vertical?(r.Bottom+25<area.Height?r.Bottom+3:r.Top-25):(r.Top>=25?r.Top-25:r.Bottom+3);y=Math.Max(2,Math.Min(area.Height-24,y));var label=new RectangleF(x,y,width,22);Visual.Fill(g,label,5,Color.FromArgb(15,22,32));Visual.Text(g,d.name,label,11,Color.FromArgb(214,221,235),false,StringAlignment.Center);}
 }
}
}


