using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Velixa {
static class Program {
 [STAThread]static void Main(string[] args){try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{Native.SetProcessDPIAware();}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Contains("--enable-startup")){Startup(true);return;}if(args.Contains("--disable-startup")){Startup(false);return;}if(args.Contains("--self-test")){SelfTest.Run();return;}if(args.Contains("--edge-test")){EdgeTest.Run();return;}if(args.Contains("--render")||args.Contains("--render-desk")){Application.Run(new MainForm(args));return;}using(var mutex=new System.Threading.Mutex(false,"Local\\VelixaDesktop")){if(!mutex.WaitOne(0)){MessageBox.Show("Velixa is already running. Open it from the system tray.","Velixa");return;}Application.Run(new MainForm(args));}}
 public static void Startup(bool enabled){using(var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(enabled)key.SetValue("Velixa","\""+Application.ExecutablePath+"\"");else key.DeleteValue("Velixa",false);}}
}
public class MainForm:Form {
 public static Color Bg=Color.FromArgb(18,20,29),PanelColor=Color.FromArgb(26,29,40),Muted=Color.FromArgb(144,151,173),Accent=Color.FromArgb(141,119,249);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr RegisterPowerSettingNotification(IntPtr handle,ref Guid setting,int flags);
 [DllImport("user32.dll")]static extern bool UnregisterPowerSettingNotification(IntPtr handle);
 [DllImport("user32.dll",SetLastError=true)]public static extern bool ChangeWindowMessageFilter(uint message,uint dwFlag);
 [DllImport("user32.dll",SetLastError=true)]public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd,uint msg,uint action,IntPtr pChangeFilterStruct);
 [DllImport("shell32.dll")]public static extern void DragAcceptFiles(IntPtr hWnd,bool fAccept);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]public static extern uint DragQueryFile(IntPtr hDrop,uint iFile,System.Text.StringBuilder lpszFile,uint cch);
 [DllImport("shell32.dll")]public static extern bool DragQueryPoint(IntPtr hDrop,out Point lppt);
 [DllImport("shell32.dll")]public static extern void DragFinish(IntPtr hDrop);
 IntPtr displayNotification;
 protected override void OnHandleCreated(EventArgs e){
  base.OnHandleCreated(e);var setting=new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");displayNotification=RegisterPowerSettingNotification(Handle,ref setting,0);
  try{ChangeWindowMessageFilter(0x233,1);ChangeWindowMessageFilter(0x4a,1);ChangeWindowMessageFilter(0x49,1);ChangeWindowMessageFilterEx(Handle,0x233,1,IntPtr.Zero);ChangeWindowMessageFilterEx(Handle,0x4a,1,IntPtr.Zero);ChangeWindowMessageFilterEx(Handle,0x49,1,IntPtr.Zero);DragAcceptFiles(Handle,true);}catch{}
 }
 protected override void OnHandleDestroyed(EventArgs e){if(displayNotification!=IntPtr.Zero){UnregisterPowerSettingNotification(displayNotification);displayNotification=IntPtr.Zero;}base.OnHandleDestroyed(e);}
 protected override void WndProc(ref Message m){
  if(m.Msg==0x233){
   IntPtr hDrop=m.WParam;
   try{
    uint count=DragQueryFile(hDrop,0xffffffff,null,0);var paths=new List<string>();var sb=new System.Text.StringBuilder(1024);
    for(uint i=0;i<count;i++){sb.Length=0;if(DragQueryFile(hDrop,i,sb,(uint)sb.Capacity)>0)paths.Add(sb.ToString());}
    Point pt;DragQueryPoint(hDrop,out pt);OnNativeDrop(pt,paths.ToArray());
   }finally{DragFinish(hDrop);}
   m.Result=IntPtr.Zero;return;
  }
  if(!closing&&(m.Msg==0x10||(m.Msg==0x112&&((m.WParam.ToInt64()&0xfff0)==0xf020||(m.WParam.ToInt64()&0xfff0)==0xf060)))){BeginInvoke((Action)(()=>{if(!IsDisposed)Hide();}));m.Result=IntPtr.Zero;return;}
  if(m.Msg==0x218&&m.WParam.ToInt32()==0x8013&&m.LParam!=IntPtr.Zero){int length=Marshal.ReadInt32(m.LParam,16);if(length==4){Network.LocalAwake=Marshal.ReadInt32(m.LParam,20)!=0;if(session!=null)session.SetAwake(Network.LocalId,Network.LocalAwake);}}
  base.WndProc(ref m);
 }
 void OnNativeDrop(Point pt,string[] paths){
  if(paths==null||paths.Length==0||session==null||sharing==null)return;
  Device target=null;
  if(desk!=null&&!desk.IsDisposed){var client=desk.PointToClient(PointToScreen(pt));target=desk.FindDropTarget(client);}
  if(target==null){var remotes=session.Devices.Where(d=>d.id!=Network.LocalId&&d.Available&&d.kind=="Windows").ToList();if(remotes.Count>0)target=remotes[0];}
  if(target!=null)sharing.SendFiles(target.id,paths);
 }
 MicrophoneShare microphone;ShareService sharing;TransferPopup transfers;Network network;DeskSession session;Receiver receiver=new Receiver();Panel content;Label status;DeskCanvas desk;Label sourceName,deskCount;SoftButton sourceButton,pauseButton,deskButton,micButton;bool closing,paused;NotifyIcon tray;Timer discovery;string[] args;int activeMicId=-1;string activeMicPeer="",activeMicName="";
 public MainForm(string[] argv){args=argv;string role=Wire.LoadSecret("role.bin","");Text="Velixa";BackColor=Bg;ForeColor=Color.White;Font=Visual.Font(14);AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;ClientSize=new Size(1080,760);MinimumSize=new Size(920,690);StartPosition=FormStartPosition.CenterScreen;string icon=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"velixa.ico");if(File.Exists(icon))Icon=new Icon(icon);
  var brand=new Panel{Dock=DockStyle.Top,Height=82};Controls.Add(brand);var logo=new PictureBox{Location=new Point(36,22),Size=new Size(32,32),SizeMode=PictureBoxSizeMode.Zoom};string png=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"velixa-logo.png");if(File.Exists(png))logo.Image=Image.FromFile(png);brand.Controls.Add(logo);LabelAt(brand,"Velixa",80,23,200,30,19,Color.White,true);var settings=Button("Settings",Settings);settings.SetBounds(920,19,124,40);settings.Anchor=AnchorStyles.Top|AnchorStyles.Right;brand.Controls.Add(settings);
  var footer=new Panel{Dock=DockStyle.Bottom,Height=40};Controls.Add(footer);status=LabelAt(footer,"Private by design. Connected on your network.",36,2,980,28,12,Muted);status.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top;
   content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(36,12,36,16)};Controls.Add(content);content.BringToFront();var menu=DarkMenu.Create();menu.Items.Add("Open Velixa",null,(s,e)=>Open());menu.Items.Add("Use this keyboard and mouse",null,(s,e)=>{if(session!=null)session.UseSource(Network.LocalId);});menu.Items.Add("Quit",null,(s,e)=>{closing=true;Close();});tray=new NotifyIcon{Icon=Icon,Text="Velixa",Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=(s,e)=>Open();FormClosing+=(s,e)=>{if(!closing&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;BeginInvoke((Action)Hide);return;}Stop();tray.Dispose();};Microsoft.Win32.SystemEvents.PowerModeChanged+=PowerChanged;FormClosed+=(s,e)=>{Microsoft.Win32.SystemEvents.PowerModeChanged-=PowerChanged;receiver.Dispose();};Visual.DarkTitle(this);Welcome();Shown+=(s,e)=>{if(args.Contains("--render")||args.Contains("--render-desk")||args.Contains("--preview-desk")){if(args.Contains("--render-desk")||args.Contains("--preview-desk")){network=new Network();session=new DeskSession(network,receiver,Ui){Preview=true};session.Devices.Add(new Device{id=Network.LocalId,name="This laptop",kind="Windows",online=true,scale=2,x=0,y=0});session.Devices.Add(new Device{id="demo-phone",name="Android phone",kind="Android",w=1080,h=2400,online=true,scale=.5,x=440,y=0});session.Devices.Add(new Device{id="demo-pc",name="Work laptop",kind="Windows",online=true,scale=.5,x=440,y=110});session.Devices.Add(new Device{id="demo-tablet",name="Tablet",kind="Android",w=1600,h=1200,online=true,scale=.5,x=440,y=172});session.Source=Network.LocalId;session.Connected=true;network.IsHost=true;DeskView();}if(args.Contains("--preview-desk")){BeginInvoke((Action)(()=>{Show();WindowState=FormWindowState.Normal;}));return;}using(var bmp=new Bitmap(Width,Height)){DrawToBitmap(bmp,new Rectangle(0,0,Width,Height));bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,args.Contains("--render-desk")?"desk.png":"welcome.png"));}closing=true;Close();}else if(role=="host")StartHost();else if(role=="receiver"&&Wire.LoadSecret("remote-token-v2.bin","")!="")StartReceiver(Wire.LoadSecret("host.bin",""),"");};
 }
 void Open(){Show();WindowState=FormWindowState.Normal;Activate();}
 public static Label LabelAt(Control p,string text,int x,int y,int w,int h,float size,Color color,bool bold=false){var l=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),ForeColor=color,Font=Visual.Font(size,bold),BackColor=Color.Transparent,UseMnemonic=false};p.Controls.Add(l);return l;}
 public static SoftButton Button(string text,Action click,bool primary=false){var b=new SoftButton{Text=text,Primary=primary,Margin=new Padding(0,0,12,0)};b.Click+=(s,e)=>click();return b;}
 static Label Copy(string value,float size=11){return new Label{Text=value,AutoSize=true,ForeColor=Muted,Font=Visual.Font(size),Margin=new Padding(0,0,0,18)};}
 void Clear(){if(discovery!=null){discovery.Stop();discovery.Dispose();discovery=null;}content.Controls.Clear();}
 void Welcome(){Clear();var stack=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Margin=Padding.Empty};stack.RowStyles.Add(new RowStyle(SizeType.Absolute,120));stack.RowStyles.Add(new RowStyle(SizeType.Absolute,272));stack.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.Controls.Add(stack);var title=new Panel{Dock=DockStyle.Fill};stack.Controls.Add(title);LabelAt(title,"A little less switching.",0,5,850,52,36,Color.White,true);LabelAt(title,"Bring your laptops, phone and tablet together. No account needed.",1,68,890,28,15,Muted);
  var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));stack.Controls.Add(row,0,1);int index=0;foreach(bool create in new[]{true,false}){var card=new SoftPanel{Dock=DockStyle.Fill,Margin=index==0?new Padding(0,0,12,0):new Padding(12,0,0,0),Padding=new Padding(26)};row.Controls.Add(card,index++,0);LabelAt(card,create?"Start a new desk":"Join a nearby desk",26,28,380,42,24,Color.White,true);var description=LabelAt(card,create?"Connect your other screens to this PC.\nChoose which laptop supplies the input anytime.":"Already set up another PC?\nFind it nearby and pair with four digits.",26,93,400,74,14,Muted);description.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;var button=Button(create?"Create my desk":"Find my desk",create?(Action)StartHost:JoinView,create);button.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom;button.SetBounds(26,card.Height-72,Math.Max(100,card.Width-52),44);card.Controls.Add(button);card.Layout+=(sender,e)=>{int bw=Math.Max(100,card.Width-52);button.SetBounds(26,card.Height-72,bw,44);};}

  var foot=new Panel{Dock=DockStyle.Fill};stack.Controls.Add(foot,0,2);LabelAt(foot,"Private, encrypted, and right here on your network.",0,28,890,28,13,Muted);status.Text="Windows sends input. Your Android touchscreen stays local.";}
 void PowerChanged(object sender,Microsoft.Win32.PowerModeChangedEventArgs e){Ui(()=>{if(e.Mode==Microsoft.Win32.PowerModes.Suspend||e.Mode==Microsoft.Win32.PowerModes.Resume){Network.LocalAwake=e.Mode!=Microsoft.Win32.PowerModes.Suspend;if(session!=null)session.SetAwake(Network.LocalId,Network.LocalAwake);}});}
 void Stop(){if(microphone!=null){microphone.Dispose();microphone=null;}if(sharing!=null){sharing.Dispose();sharing=null;}if(transfers!=null){transfers.Dispose();transfers=null;}if(session!=null){session.Dispose();session=null;}if(network!=null){network.Dispose();network=null;}receiver.Handle(Wire.Parse("{\"t\":\"leave\"}"));}
 void Ui(Action a){if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(a);}catch{}}
 void Attach(){
  session.Changed=()=>UpdateDesk();var current=session;
  sharing=new ShareService((to,body)=>{if(session==current)current.SendShared(to,body);},Ui,()=>current.Connected?current.Devices.Where(d=>d.id!=Network.LocalId&&d.Available&&d.sharing&&d.kind=="Windows").Select(d=>d.id).ToArray():new string[0]);
  sharing.ClipboardEnabled=Wire.LoadSecret("clipboard-enabled.bin","true")=="true";
  sharing.FilesEnabled=Wire.LoadSecret("files-enabled.bin","true")=="true";
  microphone=new MicrophoneShare((to,body)=>{if(session==current)current.SendShared(to,body);},Ui,id=>current.Connected&&current.Devices.Any(d=>d.id==id&&d.Available&&d.sharing));
  microphone.ReceiveEnabled=Wire.LoadSecret("mic-receive.bin","true")=="true";
  int savedOut;int.TryParse(Wire.LoadSecret("mic-output.bin","0"),out savedOut);
  microphone.OutputDevice=savedOut>=0?savedOut:(WaveAudio.Devices(false).Length>0?0:-1);
  microphone.MicsChanged=()=>UpdateDesk();
  sharing.AudioMessage=microphone.Handle;
  microphone.Status=message=>{tray.Text=(message=="Microphone sharing is on"||message=="Receiving shared microphone")?"Velixa · Microphone sharing":"Velixa";status.Text=message;UpdateDesk();};
  session.Shared=(from,body)=>sharing.Handle(from,body);
  sharing.Progress=p=>{if(transfers==null||transfers.IsDisposed)transfers=new TransferPopup();transfers.UpdateTransfer(p);};
 }
 void StartHost(){Stop();try{network=new Network();session=new DeskSession(network,receiver,Ui);Attach();network.Host();session.Host();session.SetAwake(Network.LocalId,Network.LocalAwake);Wire.SaveSecret("role.bin","host");DeskView();}catch(Exception e){Stop();MessageBox.Show(e.Message,"Could not start desk");Welcome();}}
 void StartReceiver(string host,string pin){Stop();network=new Network();var current=network;session=new DeskSession(network,receiver,Ui);Attach();network.Status=s=>Ui(()=>{if(network==current)status.Text=s;});network.Received=m=>{string type=Wire.S(m,"t");if(type=="enter"||type=="leave"||type=="move"||type=="button"||type=="key"||type=="wheel"){if(network==current&&DeskSession.ValidEvent(m))receiver.Handle(m);return;}Ui(()=>{if(network==current&&session!=null)session.Receive(m);});};network.ReconnectApproval=address=>!IsDisposed&&network==current;var r=SystemInformation.VirtualScreen;network.Connect(host,pin,r.Width,r.Height);Wire.SaveSecret("role.bin","receiver");DeskView();}
 void DeskView(){Clear();
  int savedMic;int.TryParse(Wire.LoadSecret("mic-device.bin","-1"),out savedMic);
  activeMicId=savedMic;activeMicPeer=Wire.LoadSecret("mic-peer.bin","");activeMicName=Wire.LoadSecret("mic-name.bin","");
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Margin=Padding.Empty,Padding=Padding.Empty};
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,92));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,96));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,56));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52));content.Controls.Add(layout);
  var heading=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};layout.Controls.Add(heading,0,0);deskButton=Button(session.DeskName+"  ▾",DesksMenu);deskButton.SetBounds(0,0,280,44);heading.Controls.Add(deskButton);var subhead=LabelAt(heading,"One keyboard and mouse. All your screens.",1,51,650,26,14,Muted);subhead.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;var add=Button("+  Add device",PairDialog,true);add.Width=152;add.Anchor=AnchorStyles.Top|AnchorStyles.Right;heading.Controls.Add(add);add.Location=new Point(Math.Max(0,heading.Width-add.Width),8);add.Visible=network!=null&&network.IsHost;
  var source=new SoftPanel{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,8),Padding=new Padding(22,14,20,14)};layout.Controls.Add(source,0,1);source.Paint+=(s,e)=>{var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Visual.Fill(g,new RectangleF(18,16,44,44),12,Color.FromArgb(46,41,68));using(var pen=new Pen(Color.FromArgb(185,168,255),1.4f)){g.DrawRectangle(pen,29,30,23,15);for(int x=32;x<50;x+=5)g.DrawLine(pen,x,34,x+1,34);g.DrawLine(pen,35,41,47,41);}};LabelAt(source,"Keyboard & mouse from",78,14,450,22,12,Muted);sourceName=LabelAt(source,"This laptop",78,35,600,26,16,Color.White,true);sourceName.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;sourceButton=Button("Change  ▾",SourceMenu);sourceButton.Width=122;sourceButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;source.Controls.Add(sourceButton);sourceButton.Location=new Point(source.Width-144,16);
   var micRow=new Panel{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,8)};layout.Controls.Add(micRow,0,2);micRow.Paint+=(s,e)=>{var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(Color.FromArgb(144,151,173),1.2f)){g.DrawEllipse(pen,6,12,22,22);g.DrawLine(pen,17,34,17,42);g.DrawLine(pen,9,42,25,42);g.DrawLine(pen,14,16,14,28);g.DrawLine(pen,20,16,20,28);g.DrawArc(pen,10,20,14,14,0,-180);}};LabelAt(micRow,"Microphone",36,4,200,18,12,Muted);var micLabel=LabelAt(micRow,MicName(),36,22,Math.Max(100,micRow.Width-190),22,14,Color.White,true);micLabel.AutoEllipsis=true;var micTip=new ToolTip();micTip.SetToolTip(micLabel,MicName());micButton=Button("Change  ▾",()=>MicMenu(micLabel));micButton.Width=122;micButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;micRow.Controls.Add(micButton);micButton.Location=new Point(micRow.Width-144,6);micRow.Resize+=(s,e)=>{micLabel.Width=Math.Max(100,micRow.Width-190);micButton.Location=new Point(micRow.Width-144,6);};
  desk=new DeskCanvas(session){FilesDropped=(id,paths)=>{if(sharing!=null)sharing.SendFiles(id,paths);},Dock=DockStyle.Fill,Margin=Padding.Empty};layout.Controls.Add(desk,0,3);
  var bottom=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};layout.Controls.Add(bottom,0,4);deskCount=LabelAt(bottom,"",0,16,620,24,12,Muted);deskCount.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;pauseButton=Button("Pause sharing",()=>{paused=!paused;if(paused)session.Input.Home();session.Input.Enabled=!paused&&session.Connected&&session.Source==Network.LocalId;UpdateDesk();});pauseButton.Width=152;pauseButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;bottom.Controls.Add(pauseButton);pauseButton.Location=new Point(bottom.Width-152,9);UpdateDesk();
 }
 void DesksMenu(){if(session==null)return;var menu=DarkMenu.Create();foreach(var p in session.Store.desks){string id=p.id;var item=new ToolStripMenuItem(p.name){Checked=id==session.Store.active};item.Click+=(s,e)=>{session.Profile("switch",id);DeskView();};menu.Items.Add(item);}menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Create desk…",null,(s,e)=>NameDesk("create","New desk"));menu.Items.Add("Rename desk…",null,(s,e)=>NameDesk("rename",session.DeskName));var remove=menu.Items.Add("Delete this desk",null,(s,e)=>{session.Profile("delete","");DeskView();});remove.Enabled=session.Store.desks.Count>1;menu.Show(deskButton,0,deskButton.Height);}
 void NameDesk(string action,string value){using(var f=new Form{AutoScaleMode=AutoScaleMode.None,Text=action=="create"?"Create desk":"Rename desk",ClientSize=new Size(380,170),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false}){Visual.DarkTitle(f);LabelAt(f,"Each desk keeps its layout, sizes and sleeping devices.",18,16,344,36,13,Muted);var name=new TextBox{Text=value,MaxLength=32,Location=new Point(18,60),Width=344,Font=Visual.Font(16),BackColor=PanelColor,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};f.Controls.Add(name);var save=Button("Save",()=>{session.Profile(action,name.Text);f.Close();},true);save.SetBounds(232,110,130,40);f.Controls.Add(save);f.AcceptButton=save;f.ShowDialog(this);}DeskView();}
 void SourceMenu(){if(session==null)return;var menu=DarkMenu.Create();var automatic=new ToolStripMenuItem("Automatic — use any PC's mouse or keyboard"){Checked=session.Automatic,Padding=new Padding(12,7,12,7)};automatic.Click+=(s,e)=>session.SetAutomatic(true);menu.Items.Add(automatic);menu.Items.Add(new ToolStripSeparator());foreach(var d in session.Devices.Where(d=>d.kind=="Windows")){string id=d.id;var item=new ToolStripMenuItem(d.name+(d.id==Network.LocalId?" · This PC":"")+(d.Available?"":d.sleeping?" · Sleeping":" · Unavailable")){Enabled=d.Available,Checked=!session.Automatic&&id==session.Source,Padding=new Padding(12,7,12,7)};item.Click+=(s,e)=>{paused=false;session.UseSource(id);};menu.Items.Add(item);}menu.Show(sourceButton,0,sourceButton.Height+6);}
 string MicName(){
  if(activeMicId<0)return"Microphone off";
  if(activeMicPeer==""||activeMicPeer==Network.LocalId){
   var mics=WaveAudio.FullDevices(true);
   foreach(var m in mics)if(m.Id==activeMicId)return m.Name+" ("+Environment.MachineName+")";
   return mics.Length>0?mics[0].Name+" ("+Environment.MachineName+")":"Microphone off";
  }
  List<PeerMic> list;
  if(microphone!=null&&microphone.RemoteMics.TryGetValue(activeMicPeer,out list)){
   var r=list.FirstOrDefault(m=>m.DevId==activeMicId);
   if(r!=null)return r.MicName+" ("+r.PeerName+")";
  }
  return (activeMicName!=""?activeMicName:"Microphone")+" (Connected PC)";
 }
 void MicMenu(Label label){
  var menu=DarkMenu.Create();
  var off=new ToolStripMenuItem("Microphone off"){Checked=activeMicId<0,Padding=new Padding(12,7,12,7)};
  off.Click+=(s,e)=>{
   activeMicId=-1;activeMicPeer="";activeMicName="";
   Wire.SaveSecret("mic-device.bin","-1");Wire.SaveSecret("mic-peer.bin","");Wire.SaveSecret("mic-name.bin","");
   if(microphone!=null){microphone.StopSending();microphone.StopReceiving();}
   label.Text=MicName();
  };
  menu.Items.Add(off);
  menu.Items.Add(new ToolStripSeparator());
  var localMics=WaveAudio.FullDevices(true);
  string myName=Environment.MachineName;
  foreach(var m in localMics){
   int id=m.Id;string name=m.Name;string displayName=name+" ("+myName+")";
   var item=new ToolStripMenuItem(displayName){Checked=(activeMicPeer==""||activeMicPeer==Network.LocalId)&&activeMicId==id,Padding=new Padding(12,7,12,7)};
   item.Click+=(s,e)=>{
    activeMicId=id;activeMicPeer=Network.LocalId;activeMicName=name;
    Wire.SaveSecret("mic-device.bin",id.ToString());Wire.SaveSecret("mic-peer.bin",Network.LocalId);Wire.SaveSecret("mic-name.bin",name);
    label.Text=MicName();
    StartMicToPeers(id);
   };
   menu.Items.Add(item);
  }
  if(microphone!=null&&session!=null){
   foreach(var kv in microphone.RemoteMics){
    string peerId=kv.Key;var dev=session.Devices.FirstOrDefault(d=>d.id==peerId);string peerName=dev!=null?dev.name:"Connected PC";
    foreach(var rm in kv.Value){
     int rDevId=rm.DevId;string rName=rm.MicName;string rDisplay=rName+" ("+peerName+")";
     var rItem=new ToolStripMenuItem(rDisplay){Checked=activeMicPeer==peerId&&activeMicId==rDevId,Padding=new Padding(12,7,12,7)};
     rItem.Click+=(s,e)=>{
      activeMicId=rDevId;activeMicPeer=peerId;activeMicName=rName;
      Wire.SaveSecret("mic-device.bin",rDevId.ToString());Wire.SaveSecret("mic-peer.bin",peerId);Wire.SaveSecret("mic-name.bin",rName);
      label.Text=MicName();
      microphone.RequestRemoteMic(peerId,rDevId);
     };
     menu.Items.Add(rItem);
    }
   }
  }
  menu.Show(micButton,0,micButton.Height+6);
 }
 void StartMicToPeers(int devId){
  if(microphone==null||session==null)return;
  foreach(var target in session.Devices.Where(d=>d.id!=Network.LocalId&&d.Available&&d.sharing&&d.kind=="Windows")){
   microphone.Start(target.id,devId);
  }
 }
 void UpdateDesk(){
  if(desk==null||desk.IsDisposed||session==null||sourceName==null||sourceName.IsDisposed)return;
  if(deskButton!=null&&!deskButton.IsDisposed)deskButton.Text=session.DeskName+"  ▾";
  var device=session.Devices.Find(d=>d.id==session.Source);
  sourceName.Text=device==null?"Choose a laptop":device.id==Network.LocalId?device.name+" · This PC":device.name;
  if(session.Automatic)sourceName.Text+=" · Auto";
  desk.Invalidate();
  int online=session.Devices.Count(d=>d.Available);
  deskCount.Text=session.Connected?online+" device"+(online==1?"":"s")+" ready  ·  Encrypted on your local network":"Waiting for your desk to come back online";
  pauseButton.Text=paused?"Resume sharing":"Pause sharing";
  pauseButton.Visible=session.Source==Network.LocalId&&session.Connected;
  status.Text=session.Input.Active==null?"Move across a screen edge to switch.  Ctrl + Alt + Backspace brings you back.":"You’re controlling "+session.Input.Active.Name+".  Ctrl + Alt + Backspace brings you back.";
  if(paused)session.Input.Enabled=false;
  if(microphone!=null&&session!=null&&session.Connected){
   foreach(var peer in session.Devices.Where(d=>d.id!=Network.LocalId&&d.Available&&d.sharing&&d.kind=="Windows")){
    microphone.BroadcastMics(peer.id);
   }
  }
 }
 void Settings(){using(var dialog=new Form{AutoScaleMode=AutoScaleMode.None,Text="Settings",ClientSize=new Size(480,440),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Bg,ForeColor=Color.White,Font=Font}){Visual.DarkTitle(dialog);LabelAt(dialog,"Make yourself at home",24,22,432,36,24,Color.White,true);LabelAt(dialog,"Minimize or close to keep sharing from the tray.",24,67,432,30,13,Muted);var clip=new CheckBox{Text="Share text and image clipboard",Checked=sharing!=null&&sharing.ClipboardEnabled,Enabled=sharing!=null,AutoSize=false,Location=new Point(26,111),Size=new Size(424,30),ForeColor=Color.White};clip.CheckedChanged+=(s,e)=>{sharing.ClipboardEnabled=clip.Checked;Wire.SaveSecret("clipboard-enabled.bin",clip.Checked?"true":"false");};dialog.Controls.Add(clip);var files=new CheckBox{Text="Receive dropped files in Downloads / Velixa",Checked=sharing!=null&&sharing.FilesEnabled,Enabled=sharing!=null,AutoSize=false,Location=new Point(26,148),Size=new Size(424,30),ForeColor=Color.White};files.CheckedChanged+=(s,e)=>{sharing.FilesEnabled=files.Checked;Wire.SaveSecret("files-enabled.bin",files.Checked?"true":"false");};dialog.Controls.Add(files);var mic=Button("Microphone sharing…",MicrophoneDialog);mic.Enabled=microphone!=null;mic.SetBounds(24,198,432,44);dialog.Controls.Add(mic);var lights=Button("Preview edge lights",()=>{if(session!=null)session.Input.PreviewEdges();});lights.SetBounds(24,256,432,44);dialog.Controls.Add(lights);var leave=Button("Leave this desk",()=>{dialog.Close();Stop();Wire.SaveSecret("role.bin","");Welcome();});leave.SetBounds(24,314,432,44);leave.Enabled=session!=null;dialog.Controls.Add(leave);var done=Button("Done",()=>dialog.Close(),true);done.SetBounds(328,382,128,40);dialog.Controls.Add(done);dialog.ShowDialog(this);}}
 void MicrophoneDialog(){if(microphone==null)return;using(var f=new Form{AutoScaleMode=AutoScaleMode.None,Text="Microphone sharing",ClientSize=new Size(530,570),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Bg,ForeColor=Color.White,Font=Font}){Visual.DarkTitle(f);LabelAt(f,"Your microphone, across your desk",24,20,482,34,23,Color.White,true);LabelAt(f,"Send this PC’s microphone",24,68,482,24,14,Muted,true);var inputs=new ComboBox{FlatStyle=FlatStyle.Flat,BackColor=PanelColor,ForeColor=Color.White,DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(24,102),Width=482};inputs.Items.AddRange(WaveAudio.FullDevices(true));if(inputs.Items.Count>0)inputs.SelectedIndex=0;f.Controls.Add(inputs);var peers=new ComboBox{FlatStyle=FlatStyle.Flat,BackColor=PanelColor,ForeColor=Color.White,DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(24,146),Width=482,DisplayMember="name"};var destinations=session.Devices.Where(d=>d.id!=Network.LocalId&&d.Available&&d.sharing&&d.kind=="Windows").ToList();foreach(var d in destinations)peers.Items.Add(d.name);if(peers.Items.Count>0)peers.SelectedIndex=0;else{peers.Items.Add("No compatible Windows PC is online");peers.SelectedIndex=0;peers.Enabled=false;}f.Controls.Add(peers);var start=Button("Start sharing",()=>{if(inputs.SelectedItem!=null&&peers.SelectedIndex>=0&&destinations.Count>0)microphone.Start(destinations[peers.SelectedIndex].id,((WaveAudio.Device)inputs.SelectedItem).Id);},true);start.Enabled=inputs.Items.Count>0&&destinations.Count>0;start.SetBounds(24,191,230,42);f.Controls.Add(start);var stop=Button("Stop sharing",()=>microphone.StopSending());stop.SetBounds(270,191,236,42);f.Controls.Add(stop);LabelAt(f,"Receive a microphone on this PC",24,259,482,26,16,Color.White,true);var outputs=new ComboBox{FlatStyle=FlatStyle.Flat,BackColor=PanelColor,ForeColor=Color.White,DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(24,300),Width=482};var devices=WaveAudio.FullDevices(false);outputs.Items.AddRange(devices);for(int i=0;i<devices.Length;i++)if(devices[i].Id==microphone.OutputDevice)outputs.SelectedIndex=i;if(outputs.SelectedIndex<0&&outputs.Items.Count>0)outputs.SelectedIndex=0;f.Controls.Add(outputs);var receive=new CheckBox{Text="Allow incoming microphone audio",Location=new Point(24,341),Size=new Size(482,30),Checked=microphone.ReceiveEnabled,Enabled=outputs.Items.Count>0};receive.CheckedChanged+=(s,e)=>{microphone.StopReceiving();microphone.OutputDevice=outputs.SelectedItem==null?-1:((WaveAudio.Device)outputs.SelectedItem).Id;microphone.ReceiveEnabled=receive.Checked;Wire.SaveSecret("mic-receive.bin",receive.Checked?"true":"false");if(microphone.OutputDevice>=0)Wire.SaveSecret("mic-output.bin",microphone.OutputDevice.ToString());};outputs.SelectedIndexChanged+=(s,e)=>{microphone.StopReceiving();microphone.OutputDevice=((WaveAudio.Device)outputs.SelectedItem).Id;Wire.SaveSecret("mic-output.bin",microphone.OutputDevice.ToString());};f.Controls.Add(receive);LabelAt(f,"For Teams, Zoom or another app: select a virtual cable output\nhere, then select that cable’s recording device in the app.\nSpeakers/headphones play the audio instead.",24,382,482,64,13,Muted);var guide=new LinkLabel{Text="Virtual microphone setup (VB-CABLE)",Location=new Point(24,452),Size=new Size(482,24),LinkColor=Accent};guide.LinkClicked+=(s,e)=>System.Diagnostics.Process.Start("https://vb-audio.com/Cable/");f.Controls.Add(guide);var state=LabelAt(f,microphone.State,24,495,320,48,12,Muted);var done=Button("Done",()=>f.Close());done.SetBounds(386,512,120,38);f.Controls.Add(done);using(var timer=new Timer{Interval=250}){timer.Tick+=(s,e)=>{if(microphone!=null)state.Text=microphone.State;};timer.Start();f.ShowDialog(this);}}}

 void JoinView(){Stop();Clear();var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};content.Controls.Add(panel);var title=Copy("Find your desk",32);title.ForeColor=Color.White;panel.Controls.Add(title);panel.Controls.Add(Copy("Open Add device on your main PC, then choose it below.",14));var list=new ListBox{Width=690,Height=190,BackColor=PanelColor,ForeColor=Color.White,BorderStyle=BorderStyle.None,Font=Visual.Font(16,true),DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=64};list.DrawItem+=(sender,e)=>{if(e.Index<0)return;var r=new RectangleF(e.Bounds.X+1,e.Bounds.Y+4,e.Bounds.Width-3,e.Bounds.Height-8);bool selected=(e.State&DrawItemState.Selected)!=0;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var fill=new SolidBrush(PanelColor))e.Graphics.FillRectangle(fill,e.Bounds);Visual.Fill(e.Graphics,r,10,selected?Color.FromArgb(51,43,77):Color.FromArgb(34,38,51));Visual.Text(e.Graphics,list.Items[e.Index].ToString(),new RectangleF(r.X+18,r.Y,r.Width-110,r.Height),16,Color.White,true);Visual.Text(e.Graphics,"Nearby",new RectangleF(r.Right-86,r.Y,70,r.Height),12,Color.FromArgb(153,212,192));};panel.Controls.Add(list);var note=Copy("Looking on your local network…",10);note.Margin=new Padding(0,14,0,14);panel.Controls.Add(note);var code=new TextBox{MaxLength=4,Width=200,Font=Visual.Font(32),BorderStyle=BorderStyle.FixedSingle,BackColor=PanelColor,ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};panel.Controls.Add(Copy("Four-digit code shown on your main PC",14));panel.Controls.Add(code);var pair=Button("Pair this laptop",()=>{var selected=list.SelectedItem as Nearby;if(selected==null||code.Text.Length!=4||!code.Text.All(Char.IsDigit)){note.Text="Choose a PC and enter its four-digit code.";return;}StartReceiver(selected.Address,code.Text);},true);pair.Margin=new Padding(0,18,0,12);panel.Controls.Add(pair);panel.Controls.Add(Button("Back",Welcome));bool busy=false;Action scan=async()=>{if(busy||list.IsDisposed)return;busy=true;try{var found=await Task.Run(()=>Network.Discover());if(list.IsDisposed)return;string selected=list.SelectedItem is Nearby?((Nearby)list.SelectedItem).Id:"";list.Items.Clear();foreach(var d in found.Where(v=>v[2]!=Network.LocalId)){var item=new Nearby{Name=d[1],Address=d[0],Id=d[2]};list.Items.Add(item);if(item.Id==selected)list.SelectedItem=item;}if(list.SelectedIndex<0&&list.Items.Count==1)list.SelectedIndex=0;note.Text=list.Items.Count==0?"No desks yet. Keep both PCs on the same private network.":"Choose your PC. The code expires after two minutes.";}finally{busy=false;}};discovery=new Timer{Interval=3500};discovery.Tick+=(s,e)=>scan();discovery.Start();scan();}
 class Nearby{public string Address,Name,Id;public override string ToString(){return Name;}}
 public static string[] Addresses(){try{return NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).OrderByDescending(n=>n.GetIPProperties().GatewayAddresses.Count>0).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Where(a=>a.Address.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a.Address)).Select(a=>a.Address.ToString()).Distinct().ToArray();}catch{return new string[0];}}
 void PairDialog(){if(network==null||!network.IsHost)return;network.OpenPairing();using(var dialog=new Form{AutoScaleMode=AutoScaleMode.None,Text="Add a device",ClientSize=new Size(500,580),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Bg,ForeColor=Color.White,Font=Font}){Visual.DarkTitle(dialog);LabelAt(dialog,"Make room for another screen",28,24,444,42,26,Color.White,true);LabelAt(dialog,"Keep both devices on the same network.",28,74,444,26,14,Muted);var body=new Panel{Location=new Point(28,168),Size=new Size(444,340)};dialog.Controls.Add(body);bool android=false;var windows=Button("Windows PC",()=>{},true);var phone=Button("Android",()=>{});windows.SetBounds(28,115,216,42);phone.SetBounds(256,115,216,42);dialog.Controls.Add(windows);dialog.Controls.Add(phone);var addresses=Addresses();string address=addresses.FirstOrDefault();Label countdown=LabelAt(dialog,"",28,517,444,26,12,Muted);countdown.TextAlign=ContentAlignment.MiddleCenter;Action show=()=>{foreach(var image in body.Controls.OfType<PictureBox>()){var img=image.Image;image.Image=null;if(img!=null)img.Dispose();}body.Controls.Clear();windows.Primary=!android;phone.Primary=android;windows.Invalidate();phone.Invalidate();if(android){if(address==null){LabelAt(body,"Connect this PC to your local network first.",0,80,444,70,16,Muted);return;}var qr=new PictureBox{Location=new Point(90,6),Size=new Size(264,264),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.White};using(var gen=new QRCoder.QRCodeGenerator())using(var data=gen.CreateQrCode(network.Qr(address),QRCoder.QRCodeGenerator.ECCLevel.M))using(var code=new QRCoder.QRCode(data))qr.Image=code.GetGraphic(8);body.Controls.Add(qr);var text=LabelAt(body,"Open Velixa on Android and tap Scan QR code.",0,291,444,30,14,Muted);text.TextAlign=ContentAlignment.MiddleCenter;}else{var hint=LabelAt(body,"On your other PC, choose Join a nearby desk.\nSelect "+Environment.MachineName+" and enter this code.",6,25,432,64,15,Muted);hint.TextAlign=ContentAlignment.MiddleCenter;var box=new SoftPanel{Location=new Point(24,117),Size=new Size(396,116)};body.Controls.Add(box);box.Paint+=(s,e)=>{Visual.Text(e.Graphics,network.Code,new RectangleF(0,0,box.Width,box.Height),54,Color.FromArgb(230,223,255),true,StringAlignment.Center);};var caption=LabelAt(body,"The code pairs one device, then disappears.",0,268,444,30,13,Muted);caption.TextAlign=ContentAlignment.MiddleCenter;}};windows.Click+=(s,e)=>{android=false;show();};phone.Click+=(s,e)=>{android=true;show();};show();var timer=new Timer{Interval=500};timer.Tick+=(s,e)=>{int seconds=(int)(network.PairUntil-DateTime.UtcNow).TotalSeconds;if(seconds<=0){timer.Stop();foreach(var image in body.Controls.OfType<PictureBox>()){var img=image.Image;image.Image=null;if(img!=null)img.Dispose();}body.Controls.Clear();var label=LabelAt(body,"Pairing closed",0,80,444,50,26,Color.White,true);label.TextAlign=ContentAlignment.MiddleCenter;var note=LabelAt(body,"Your device has paired or the code has expired.\nOpen Add device again to pair another screen.",0,142,444,68,14,Muted);note.TextAlign=ContentAlignment.MiddleCenter;windows.Enabled=phone.Enabled=false;countdown.Text="";}else countdown.Text="Available for "+seconds+" seconds";};timer.Start();dialog.ShowDialog(this);timer.Dispose();foreach(var image in body.Controls.OfType<PictureBox>()){var img=image.Image;image.Image=null;if(img!=null)img.Dispose();}network.ClosePairing();}}

}
public class DeskCanvas:Control {
 readonly DeskSession session;readonly ToolTip deviceTip=new ToolTip();Device hovered,dropHover;public Device Selected;public Action SelectionChanged;public Action<string,string[]> FilesDropped;Point start;double originalX,originalY;bool moved;float scale,ox,oy;
 public DeskCanvas(DeskSession s){
  session=s;AllowDrop=true;
  DragEnter+=(sender,e)=>e.Effect=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;
  DragOver+=(sender,e)=>{
   if(!e.Data.GetDataPresent(DataFormats.FileDrop)){e.Effect=DragDropEffects.None;return;}
   var point=PointToClient(new Point(e.X,e.Y));
   var target=FindDropTarget(point);
   if(target!=dropHover){dropHover=target;Invalidate();}
   e.Effect=target!=null?DragDropEffects.Copy:DragDropEffects.None;
  };
  DragLeave+=(sender,e)=>{if(dropHover!=null){dropHover=null;Invalidate();}};
  DragDrop+=(sender,e)=>{
   var point=PointToClient(new Point(e.X,e.Y));
   var target=FindDropTarget(point);
   dropHover=null;Invalidate();
   if(target!=null&&FilesDropped!=null&&e.Data.GetDataPresent(DataFormats.FileDrop))
    FilesDropped(target.id,(string[])e.Data.GetData(DataFormats.FileDrop));
  };
  DoubleBuffered=true;BackColor=MainForm.PanelColor;Cursor=Cursors.Hand;SetStyle(ControlStyles.ResizeRedraw,true);
 }
 protected override void OnHandleCreated(EventArgs e){
  base.OnHandleCreated(e);
  try{MainForm.ChangeWindowMessageFilterEx(Handle,0x233,1,IntPtr.Zero);MainForm.DragAcceptFiles(Handle,true);}catch{}
 }
 protected override void WndProc(ref Message m){
  if(m.Msg==0x233){
   IntPtr hDrop=m.WParam;
   try{
    uint count=MainForm.DragQueryFile(hDrop,0xffffffff,null,0);var paths=new List<string>();var sb=new System.Text.StringBuilder(1024);
    for(uint i=0;i<count;i++){sb.Length=0;if(MainForm.DragQueryFile(hDrop,i,sb,(uint)sb.Capacity)>0)paths.Add(sb.ToString());}
    Point pt;MainForm.DragQueryPoint(hDrop,out pt);var target=FindDropTarget(pt);
    if(target!=null&&FilesDropped!=null&&paths.Count>0)FilesDropped(target.id,paths.ToArray());
   }finally{MainForm.DragFinish(hDrop);}
   m.Result=IntPtr.Zero;return;
  }
  base.WndProc(ref m);
 }
 public Device FindDropTarget(Point point){
  Transform();
  var remotes=session.Devices.Where(d=>d.id!=Network.LocalId&&d.Available&&d.kind=="Windows").ToList();
  if(remotes.Count==0)return null;
  var direct=remotes.LastOrDefault(d=>Card(d).Contains(point));
  if(direct!=null)return direct;
  if(remotes.Count==1)return remotes[0];
  return remotes.OrderBy(d=>{var c=Card(d);float cx=c.X+c.Width/2f-point.X,cy=c.Y+c.Height/2f-point.Y;return cx*cx+cy*cy;}).FirstOrDefault();
 }
 void Transform(){var d=session.Devices;if(d.Count==0){scale=1;ox=40;oy=60;return;}float left=d.Min(v=>v.Bounds.Left),right=d.Max(v=>v.Bounds.Right),top=d.Min(v=>v.Bounds.Top),bottom=d.Max(v=>v.Bounds.Bottom);float contentW=right-left+60,contentH=bottom-top+60;float availW=Math.Max(100,Width-60),availH=Math.Max(100,Height-120);scale=Math.Min(1.6f,Math.Min(availW/contentW,availH/contentH));ox=Width/2f-(left+right)/2*scale;oy=50+(availH)/2f-(top+bottom)/2*scale;}
 public RectangleF Card(Device d){var r=d.Bounds;return new RectangleF(ox+r.X*scale,oy+r.Y*scale,r.Width*scale,r.Height*scale);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(!Capture)Transform();var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(MainForm.Bg);Visual.Fill(g,new RectangleF(0,0,Width,Height),20,MainForm.PanelColor);Visual.Stroke(g,new RectangleF(.5f,.5f,Width-1,Height-1),20,Color.FromArgb(43,47,61));Visual.Text(g,Selected==null?"Arrange your screens":Selected.name+" · "+(Selected.sleeping?"Sleeping":Selected.Available?"Ready":"Unavailable")+" · "+(int)(Selected.scale*100)+"%",new RectangleF(24,15,Width-48,30),13,Color.FromArgb(190,195,211),true);
   foreach(var d in session.Devices){var r=Card(d);bool phone=d.kind=="Android";float radius=phone?Math.Min(16,r.Width*.15f):8;var shadow=r;shadow.Offset(0,5);shadow.Inflate(2,0);Visual.Fill(g,shadow,radius+2,Color.FromArgb(20,22,31));}
   foreach(var d in session.Devices){var r=Card(d);bool source=d.id==session.Source,phone=d.kind=="Android",online=d.Available;float radius=phone?Math.Min(16,r.Width*.15f):8;Visual.Fill(g,r,radius,Color.FromArgb(online?49:32,online?49:35,online?68:46));var screen=r;screen.Inflate(phone?-6:-7,phone?-9:-7);if(screen.Width>1&&screen.Height>1){using(var path=Visual.Round(screen,Math.Max(3,radius-5)))using(var gradient=new LinearGradientBrush(screen,!online?Color.FromArgb(34,37,49):source?Color.FromArgb(66,49,105):Color.FromArgb(38,55,81),!online?Color.FromArgb(29,32,42):source?Color.FromArgb(35,35,64):Color.FromArgb(30,34,53),45)){g.FillPath(gradient,path);}if(online){var state=g.Save();g.SetClip(Visual.Round(screen,Math.Max(3,radius-5)));using(var arc=new Pen(Color.FromArgb(28,173,155,235),Math.Max(16,screen.Width*.17f)))g.DrawEllipse(arc,screen.X+screen.Width*.28f,screen.Y-screen.Height*.38f,screen.Width,screen.Height*1.7f);g.Restore(state);}}if(phone){using(var pen=new Pen(Color.FromArgb(107,113,138),2)){g.DrawLine(pen,r.X+r.Width*.37f,r.Y+7,r.X+r.Width*.63f,r.Y+7);g.DrawLine(pen,r.X+r.Width*.33f,r.Bottom-6,r.X+r.Width*.67f,r.Bottom-6);}}float textSize=Math.Max(9,Math.Min(14,r.Width/10));Visual.Text(g,d.name,new RectangleF(r.X+8,r.Y+r.Height/2-(r.Height>=70?23:12),r.Width-16,24),textSize,online?Color.White:MainForm.Muted,true,StringAlignment.Center);if(r.Height>=70)Visual.Text(g,d.sleeping?"Sleeping":!d.online?"Offline":!d.awake?"Screen asleep":source?"Input source":"Ready",new RectangleF(r.X+6,r.Y+r.Height/2+1,r.Width-12,21),Math.Max(9,textSize-2),MainForm.Muted,false,StringAlignment.Center);Color border=!online?Color.FromArgb(62,66,80):source?Color.FromArgb(157,134,251):Color.FromArgb(107,113,138);Visual.Stroke(g,r,radius,border,source&&online?2f:1.5f);if(!d.online&&d.id!=Network.LocalId){var xRect=new RectangleF(r.Right-20,r.Top+6,14,14);using(var pen=new Pen(Color.FromArgb(140,145,165),1.8f)){g.DrawLine(pen,xRect.Left+2,xRect.Top+2,xRect.Right-2,xRect.Bottom-2);g.DrawLine(pen,xRect.Right-2,xRect.Top+2,xRect.Left+2,xRect.Bottom-2);}}}
   var drawnPairs=new HashSet<string>();foreach(var d in session.Devices.Where(v=>v.Available)){var r1=Card(d);foreach(var n in session.Devices.Where(v=>v!=d&&v.Available)){string pairKey=string.Compare(d.id,n.id)<0?d.id+":"+n.id:n.id+":"+d.id;if(!drawnPairs.Add(pairKey))continue;var r2=Card(n);foreach(string side in new[]{"left","right","top","bottom"}){double from,to;if(DeskGeometry.Segment(d.Bounds,n.Bounds,side,out from,out to)){float lo=(float)from,hi=(float)to;using(var pen=new Pen(Color.FromArgb(180,151,255),3f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;if(side=="right"){float seamX=(r1.Right+r2.Left)/2f;g.DrawLine(pen,seamX,r1.Top+r1.Height*lo+3,seamX,r1.Top+r1.Height*hi-3);}else if(side=="bottom"){float seamY=(r1.Bottom+r2.Top)/2f;g.DrawLine(pen,r1.Left+r1.Width*lo+3,seamY,r1.Left+r1.Width*hi-3,seamY);}}}}}}
   if(dropHover!=null){var r=Card(dropHover);bool phone=dropHover.kind=="Android";float radius=phone?Math.Min(16,r.Width*.15f):8;Visual.Stroke(g,r,radius,Color.FromArgb(180,151,255),3f);}else if(Selected!=null){var r=Card(Selected);bool phone=Selected.kind=="Android";float radius=phone?Math.Min(16,r.Width*.15f):8;Visual.Stroke(g,r,radius,Color.FromArgb(185,160,255),2.5f);}
   string hint=dropHover!=null?"Drop files to send to "+dropHover.name:(session.Devices.Count<=1?"Add your other devices to bring your desk together.":"Drag screens to arrange · Click for options · Drop files onto a Windows device to send");Visual.Text(g,hint,new RectangleF(24,Height-43,Width-48,28),12,dropHover!=null?Color.FromArgb(200,185,255):MainForm.Muted,false,StringAlignment.Center);
  }
  static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);var clicked=session.Devices.LastOrDefault(d=>Card(d).Contains(e.Location));if(clicked!=null&&!clicked.online&&clicked.id!=Network.LocalId){var cr=Card(clicked);if(e.X>=cr.Right-24&&e.X<=cr.Right&&e.Y>=cr.Top&&e.Y<=cr.Top+24){session.RemoveDevice(clicked.id);Invalidate();return;}}Selected=clicked;start=e.Location;moved=false;if(Selected!=null){originalX=Selected.x;originalY=Selected.y;Capture=e.Button==MouseButtons.Left;}if(SelectionChanged!=null)SelectionChanged();Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!Capture){var over=session.Devices.LastOrDefault(d=>Card(d).Contains(e.Location));if(over!=hovered){hovered=over;deviceTip.SetToolTip(this,over==null?"":over.name+" · "+(over.sleeping?"Sleeping":over.Available?"Ready":"Unavailable")+" · Click for options");}}if(!Capture||Selected==null)return;if(Math.Abs(e.X-start.X)+Math.Abs(e.Y-start.Y)>6)moved=true;if(moved){var point=DeskGeometry.Snap(new RectangleF((float)(originalX+(e.X-start.X)/scale),(float)(originalY+(e.Y-start.Y)/scale),Selected.Bounds.Width,Selected.Bounds.Height),session.Devices.Where(d=>d!=Selected).Select(d=>d.Bounds));Selected.x=point.X;Selected.y=point.Y;Invalidate();if(SelectionChanged!=null)SelectionChanged();}}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;if(Selected!=null){if(moved){double x=Selected.x,y=Selected.y;Selected.x=originalX;Selected.y=originalY;session.Position(Selected.id,x,y);}else Options(e.Location);}Invalidate();}
 protected override void Dispose(bool disposing){if(disposing)deviceTip.Dispose();base.Dispose(disposing);}
 public static Color DeviceColor(string id){uint hash=0;foreach(char c in id??"")hash=hash*31+c;return new[]{Color.FromArgb(102,218,222),Color.FromArgb(180,151,255),Color.FromArgb(250,181,111),Color.FromArgb(139,216,156),Color.FromArgb(242,148,190),Color.FromArgb(126,179,251)}[hash%6];}
 void Options(Point point){var d=Selected;if(d==null)return;var menu=DarkMenu.Create();menu.Items.Add(d.name).Enabled=false;menu.Items.Add(d.sleeping?"Wake in Velixa":"Put to sleep in Velixa",null,(s,e)=>session.SleepDevice(d.id,!d.sleeping));menu.Items.Add("Adjust screen size…",null,(s,e)=>SizeDialog(d));var use=menu.Items.Add("Use this keyboard and mouse",null,(s,e)=>session.UseSource(d.id));use.Enabled=d.Available&&d.kind=="Windows";if(d.id!=Network.LocalId){menu.Items.Add(new ToolStripSeparator());var remove=menu.Items.Add("Remove from desk",null,(s,e)=>session.RemoveDevice(d.id));remove.Enabled=!d.online;}menu.Show(this,point);}
 void SizeDialog(Device d){using(var f=new Form{AutoScaleMode=AutoScaleMode.None,Text="Screen size · "+d.name,ClientSize=new Size(470,315),StartPosition=FormStartPosition.CenterParent,BackColor=MainForm.Bg,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){Visual.DarkTitle(f);MainForm.LabelAt(f,"Match the size on your desk",22,18,406,35,22,Color.White,true);MainForm.LabelAt(f,"Keeps the screen's proportions. Drag again to adjust its edge.\n100% = standard screen; use smaller sizes for phones.",22,60,406,52,13,MainForm.Muted);var slider=new TrackBar{AutoSize=false,Height=36,Minimum=25,Maximum=300,TickFrequency=25,Value=(int)Math.Max(25,Math.Min(300,d.scale*100)),Location=new Point(22,125),Width=422};f.Controls.Add(slider);var label=MainForm.LabelAt(f,slider.Value+"%",24,178,180,30,16,Color.White,true);slider.ValueChanged+=(s,e)=>label.Text=slider.Value+"%";var presets=DarkMenu.Create();foreach(var choice in new[]{new{n="Phone · 40%",v=40},new{n="Tablet · 65%",v=65},new{n="Laptop · 100%",v=100},new{n="Large monitor · 160%",v=160}}){int v=choice.v;presets.Items.Add(choice.n,null,(s,e)=>slider.Value=v);}var preset=MainForm.Button("Presets ▾",()=>presets.Show(f,new Point(22,215)));preset.SetBounds(22,247,142,44);f.Controls.Add(preset);var apply=MainForm.Button("Apply",()=>{session.ResizeDevice(d.id,slider.Value/100d);f.Close();},true);apply.SetBounds(306,247,142,44);f.Controls.Add(apply);f.AcceptButton=apply;f.ShowDialog(this);presets.Dispose();}}

}
public static class EdgeTest {
 public static void Run(){using(var form=new Form()){form.Shown+=(s,e)=>{foreach(var screen in Screen.AllScreens){var b=screen.Bounds;foreach(var r in new[]{new Rectangle(b.Left,b.Top,6,b.Height),new Rectangle(b.Right-6,b.Top,6,b.Height),new Rectangle(b.Left,b.Top,b.Width,6),new Rectangle(b.Left,b.Bottom-6,b.Width,6)})using(var edge=new Edge(r,Color.Cyan)){edge.Show();Application.DoEvents();Native.RECT actual;Native.GetWindowRect(edge.Handle,out actual);if(actual.Left!=r.Left||actual.Top!=r.Top||actual.Right!=r.Right||actual.Bottom!=r.Bottom)throw new Exception("Overlay bounds mismatch: "+r+" vs "+actual.Left+","+actual.Top+","+actual.Right+","+actual.Bottom);}}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"edge-test.txt"),"All four native overlay bounds match every monitor exactly.");form.Close();};Application.Run(form);}}
}
}


