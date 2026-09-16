using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Velixa {
public partial class MainForm {
 Label connectionLabel,selectedName,deviceBadge; ToggleSwitch inputToggle,clipboardToggle;
 void BuildChrome(){
  var brand=new Panel{Dock=DockStyle.Top,Height=78};Controls.Add(brand);
  brand.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,"logo",new RectangleF(23,18,38,38),Accent);using(var p=new Pen(Color.FromArgb(28,37,51)))e.Graphics.DrawLine(p,0,77,brand.Width,77);};
  LabelAt(brand,"Velixa",78,12,180,32,26,Color.White,true);LabelAt(brand,"One input. Everywhere.",79,46,220,22,13,Muted);
  var settings=Button("",()=>{Settings();RefreshInspector();});settings.Name="LegacyHeaderSettings";settings.Width=40;settings.IconName="settings";settings.Paint+=(s,e)=>UiIcons.Draw(e.Graphics,"settings",new RectangleF(10,10,20,20),Muted);settings.Visible=false;brand.Controls.Add(settings);
  var more=Button("",()=>{var menu=DarkMenu.Create();menu.Items.Add("Open received files",null,(s,e)=>OpenReceivedFiles());menu.Items.Add("Hide to tray",null,(s,e)=>Hide());menu.Items.Add("Quit Velixa",null,(s,e)=>{closing=true;Close();});menu.Show(brand,new Point(brand.Width-200,64));});more.IconName="more";more.Paint+=(s,e)=>UiIcons.Draw(e.Graphics,"more",new RectangleF(10,10,20,20),Muted);brand.Controls.Add(more);
  var badge=new SoftPanel{Radius=20,FillColor=PanelColor};brand.Controls.Add(badge);badge.Paint+=(s,e)=>{using(var b=new SolidBrush(session!=null&&session.Connected?Color.FromArgb(57,202,159):Muted))e.Graphics.FillEllipse(b,14,14,12,12);};connectionLabel=LabelAt(badge,"Not connected",34,11,106,22,12,Color.White);
  Action place=()=>{more.SetBounds(brand.Width-56,20,40,40);settings.SetBounds(brand.Width-110,20,40,40);badge.SetBounds(brand.Width-268,20,142,40);};brand.Resize+=(s,e)=>place();place();
  var footer=new Panel{Dock=DockStyle.Bottom,Height=40};Controls.Add(footer);footer.Paint+=(s,e)=>{using(var p=new Pen(Color.FromArgb(28,37,51)))e.Graphics.DrawLine(p,0,0,footer.Width,0);using(var b=new SolidBrush(session!=null&&session.Connected?Color.FromArgb(57,202,159):Muted))e.Graphics.FillEllipse(b,20,16,12,12);};
  status=LabelAt(footer,"Private by design. Connected on your network.",42,12,1000,22,12,Muted);status.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top;
 }
 void ToggleInput(){if(session==null)return;paused=!paused;if(paused)session.Input.Home();session.Input.Enabled=!paused&&session.Connected&&(session.Automatic||session.Source==Network.LocalId);UpdateDesk();}
 void Info(string title,string text){using(var f=new Form{Text=title,ClientSize=new Size(450,210),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){Visual.DarkTitle(f);LabelAt(f,title,24,20,400,35,24,Color.White,true);LabelAt(f,text,24,69,400,80,14,Muted);var done=Button("Done",()=>f.Close(),true);done.SetBounds(300,156,126,36);f.Controls.Add(done);f.ShowDialog(this);}}
 void BuildDesk(){
  activeSection="My Desk";Clear();int savedMic;int.TryParse(Wire.LoadSecret("mic-device.bin","-1"),out savedMic);activeMicId=savedMic;activeMicPeer=Wire.LoadSecret("mic-peer.bin","");activeMicName=Wire.LoadSecret("mic-name.bin","");
  var root=new Panel{Dock=DockStyle.Fill};content.Controls.Add(root);
  var nav=new SoftPanel{FillColor=Color.FromArgb(15,21,31)};var center=new SoftPanel{FillColor=Color.FromArgb(17,23,34)};var right=new Panel{BackColor=Bg};root.Controls.AddRange(new Control[]{nav,center,right});
  var home=new NavigationButton("My Desk","monitor",true);home.Click+=(s,e)=>ShowSection("My Desk");nav.Controls.Add(home);
  var devices=new NavigationButton("Devices","devices",false);devices.Click+=(s,e)=>ShowSection("Devices");nav.Controls.Add(devices);
  var settings=new NavigationButton("Settings","settings",false);settings.Name="Settings";settings.Click+=(s,e)=>ShowSection("Settings");nav.Controls.Add(settings);
  var hotkeys=new NavigationButton("Hotkeys","grid",false);hotkeys.Click+=(s,e)=>ShowSection("Hotkeys");nav.Controls.Add(hotkeys);
  var about=new NavigationButton("About","info",false);about.Click+=(s,e)=>ShowSection("About");nav.Controls.Add(about);
  deviceBadge=LabelAt(devices,"",devices.Width-40,13,28,22,12,Color.White,true);deviceBadge.Visible=false;deviceBadge.TextAlign=ContentAlignment.MiddleCenter;deviceBadge.BackColor=Color.FromArgb(38,47,67);deviceBadge.Anchor=AnchorStyles.Top|AnchorStyles.Right;
  var slogan=new SoftPanel{FillColor=Color.FromArgb(22,29,41)};nav.Controls.Add(slogan);slogan.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,"logo",new RectangleF((slogan.Width-42)/2,18,42,42),Accent);Visual.Text(e.Graphics,"Move. Type. Work.",new RectangleF(0,72,slogan.Width,24),14,Muted,false,StringAlignment.Center);Visual.Text(e.Graphics,"Across devices.",new RectangleF(0,96,slogan.Width,24),14,Muted,false,StringAlignment.Center);};
  var title=LabelAt(center,"Your Desk",24,21,350,40,30,Color.White,true);var sub=LabelAt(center,"Arrange your devices to match your physical setup.\nMove your cursor across the screens seamlessly.",24,65,600,48,15,Muted);
  var add=new DeskActionButton{Text="Add device",Primary=true};add.Click+=(s,e)=>PairDialog();add.Name="AddDevice";add.Visible=network!=null&&network.IsHost;center.Controls.Add(add);deskButton=new DeskActionButton{Text="Desks"};deskButton.Click+=(s,e)=>DesksMenu();center.Controls.Add(deskButton);
  desk=new DeskCanvas(session){Name="Arrangement",FilesDropped=(id,paths)=>{if(sharing!=null)sharing.SendFiles(id,paths);}};desk.Selected=session.Devices.FirstOrDefault(d=>d.id==Network.LocalId);desk.SelectionChanged=RefreshInspector;center.Controls.Add(desk);
  var hint=new SoftPanel{FillColor=Color.FromArgb(24,31,45)};center.Controls.Add(hint);hint.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,"cursor",new RectangleF(17,14,18,18),Accent);Visual.Text(e.Graphics,"Drag devices to arrange  ·  Your cursor follows this layout",new RectangleF(45,0,hint.Width-58,hint.Height),12,Color.FromArgb(226,231,242),false,StringAlignment.Center);};
  var input=InspectorPanel(right,"Input sharing","bluetooth");var device=InspectorPanel(right,"Device settings","monitor");var screen=InspectorPanel(right,"Screen layout","devices");
  IconLabel(input,"Input follows your source","keyboard",64);IconLabel(input,"Share clipboard","clipboard",112);
  inputToggle=new ToggleSwitch{Checked=!paused,AccessibleName="Keyboard and mouse sharing"};inputToggle.Click+=(s,e)=>ToggleInput();inputToggle.Visible=false;input.Controls.Add(inputToggle);
  clipboardToggle=new ToggleSwitch{Checked=sharing!=null&&sharing.ClipboardEnabled,Enabled=sharing!=null,AccessibleName="Share clipboard"};clipboardToggle.Click+=(s,e)=>{if(sharing==null)return;sharing.ClipboardEnabled=!sharing.ClipboardEnabled;Wire.SaveSecret("clipboard-enabled.bin",sharing.ClipboardEnabled?"true":"false");RefreshInspector();};input.Controls.Add(clipboardToggle);
  LabelAt(device,"Name",18,60,180,22,14,Muted);var nameBox=new SoftPanel{Radius=9,FillColor=Color.FromArgb(25,33,47)};device.Controls.Add(nameBox);selectedName=LabelAt(nameBox,"",10,11,180,24,13,Color.White);selectedName.AutoEllipsis=true;
  LabelAt(device,"Input mode",18,135,190,22,14,Muted);sourceButton=Button("Automatic (recommended)",SourceMenu);device.Controls.Add(sourceButton);sourceName=LabelAt(device,"",18,204,220,22,12,Muted);sourceName.AutoEllipsis=true;
  var options=Button("Device actions   ›",()=>desk.ShowSelectedOptions());options.Name="DeviceActions";device.Controls.Add(options);
  var zoomOut=Button("−",()=>desk.Zoom(-.2f));zoomOut.AccessibleName="Zoom out";zoomOut.SetBounds(18,56,40,38);screen.Controls.Add(zoomOut);var zoomIn=Button("+",()=>desk.Zoom(.2f));zoomIn.AccessibleName="Zoom in";zoomIn.SetBounds(68,56,40,38);screen.Controls.Add(zoomIn);var fit=Button("Fit",()=>desk.Fit());screen.Controls.Add(fit);
  micButton=Button("Microphone",()=>MicMenu(micLabel));nav.Controls.Add(micButton);micButton.AccessibleName="Choose microphone";
  micLabel=LabelAt(nav,microphone==null?"No microphone session":microphone.State,8,0,210,22,12,Muted);micLabel.AutoEllipsis=true;
  pauseButton=Button("Pause sharing",ToggleInput);right.Controls.Add(pauseButton);deskCount=new Label();
  Action layout=()=>{
   bool compact=root.Width<1150;int navW=compact?160:198,rightW=compact?240:278,gap=18;nav.SetBounds(0,0,navW,root.Height);right.SetBounds(root.Width-rightW,0,rightW,root.Height);center.SetBounds(navW+gap,0,Math.Max(300,root.Width-navW-rightW-gap*2),root.Height);
   int panelW=rightW-(root.Height<678?SystemInformation.VerticalScrollBarWidth:0);int y=14;foreach(var b in new[]{home,devices,settings,hotkeys,about}){b.SetBounds(12,y,navW-24,44);y+=51;}slogan.SetBounds(12,nav.Height-144,navW-24,132);
   bool narrow=center.Width<640;title.SetBounds(24,20,narrow?center.Width-48:center.Width-320,40);sub.SetBounds(24,narrow?65:78,center.Width-48,48);int actionY=narrow?120:30;add.SetBounds(center.Width-276,actionY,128,44);deskButton.SetBounds(center.Width-134,actionY,110,44);add.BringToFront();deskButton.BringToFront();
   int canvasY=narrow?178:132;desk.SetBounds(24,canvasY,center.Width-48,Math.Max(180,center.Height-canvasY-98));hint.SetBounds(Math.Max(24,(center.Width-510)/2),center.Height-72,Math.Min(510,center.Width-48),46);
   input.SetBounds(0,16,panelW,158);device.SetBounds(0,192,panelW,292);screen.SetBounds(0,502,panelW,112);
   inputToggle.SetBounds(panelW-58,65,40,24);clipboardToggle.SetBounds(panelW-58,113,40,24);nameBox.SetBounds(18,87,panelW-36,38);selectedName.Width=nameBox.Width-20;sourceButton.SetBounds(18,162,panelW-36,40);sourceName.Width=panelW-36;sourceButton.Text=session.Automatic?(panelW<250?"Automatic":"Automatic (recommended)"):"Choose input source";options.SetBounds(18,238,panelW-36,36);fit.SetBounds(panelW-72,56,54,38);
   int bottom=Math.Max(630,right.Height-48);pauseButton.SetBounds(0,bottom,panelW,48);micButton.SetBounds(12,282,navW-24,36);micLabel.SetBounds(18,325,navW-36,40);
   right.AutoScrollMinSize=new Size(0,678);right.AutoScroll=true;

  };root.Resize+=(s,e)=>layout();layout();sectionHost=root;deskCenter=center;deskInspector=right;navItems=new[]{home,devices,settings,hotkeys,about};sectionPage=new SoftPanel{Visible=false,AutoScroll=true,FillColor=PanelColor};root.Controls.Add(sectionPage);root.Resize+=(s,e)=>LayoutSection();UpdateDesk();
 }
 Label micLabel;
 SoftPanel InspectorPanel(Control parent,string title,string icon){var p=new SoftPanel{FillColor=PanelColor};parent.Controls.Add(p);p.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,icon,new RectangleF(18,20,22,22),Color.FromArgb(91,145,255));Visual.Text(e.Graphics,title,new RectangleF(55,15,p.Width-65,32),15,Color.White,true);};return p;}
 void IconLabel(Control p,string text,string icon,int y){p.Paint+=(s,e)=>{UiIcons.Draw(e.Graphics,icon,new RectangleF(18,y+3,19,19),Muted);Visual.Text(e.Graphics,text,new RectangleF(47,y,p.Width-106,27),p.Width<250?11:13,Color.FromArgb(228,234,245));};}
 void RefreshInspector(){if(session==null||selectedName==null||selectedName.IsDisposed)return;var selected=desk.Selected;if(selected!=null&&!session.Devices.Contains(selected))desk.Selected=selected=null;var actions=deskInspector.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).FirstOrDefault(c=>c.Name=="DeviceActions");if(actions!=null)actions.Enabled=selected!=null&&session.Connected;sourceButton.Enabled=deskButton.Enabled=session.Connected;selectedName.Text=selected==null?"Select a device":selected.name+(selected.id==Network.LocalId?" · This PC":"");sourceButton.Text=session.Automatic?(sourceButton.Width<210?"Automatic":"Automatic (recommended)"):"Choose input source";deviceBadge.Text=session.Devices.Count.ToString();inputToggle.Checked=!paused;inputToggle.Invalidate();clipboardToggle.Enabled=sharing!=null;clipboardToggle.Checked=sharing!=null&&sharing.ClipboardEnabled;clipboardToggle.Invalidate();connectionLabel.Text=session.Connected?"Connected":"Reconnecting";connectionLabel.Parent.Invalidate();int online=session.Devices.Count(d=>d.id!=Network.LocalId&&d.Available);status.Text=online+" device"+(online==1?"":"s")+" connected     ·     Ctrl + Alt + Backspace to return";pauseButton.Visible=true;pauseButton.Enabled=session.Connected&&(session.Automatic||session.Source==Network.LocalId);}
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
  int number=0;foreach(var d in session.Devices){number++;var r=card(d);r.Inflate(-7,-7);if(r.Width<8||r.Height<8)continue;bool local=d.id==Network.LocalId,phone=d.kind=="Android";Color border=local?Color.FromArgb(255,197,67):Color.FromArgb(113,102,255);var saved=g.Save();using(var clip=Visual.Round(r,12)){g.SetClip(clip);using(var bg=new LinearGradientBrush(r,local?Color.FromArgb(8,27,53):Color.FromArgb(17,30,73),local?Color.FromArgb(35,87,122):Color.FromArgb(71,44,123),65))g.FillRectangle(bg,r);
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
 }
}
}
