using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Velixa {
public partial class MainForm {
 Panel sectionHost;SoftPanel deskCenter,sectionPage;Panel deskInspector;NavigationButton[] navItems;string activeSection="My Desk";
 string devicePageState="";
 void RefreshDevicePage(){if(activeSection!="Devices"||sectionPage==null||sectionPage.IsDisposed||!sectionPage.Visible)return;string state=String.Join("|",session.Devices.Select(d=>d.id+":"+d.name+":"+d.Available+":"+d.sleeping+":"+d.scale));if(state==devicePageState)return;devicePageState=state;var scroll=sectionPage.AutoScrollPosition;ShowSection("Devices");sectionPage.AutoScrollPosition=new Point(-scroll.X,-scroll.Y);}

 void AddPreference(ref int y,string title,string text,bool value,bool enabled,Action<bool> changed){var row=new SoftPanel{Location=new Point(24,y),Size=new Size(sectionPage.ClientSize.Width-48,112),FillColor=Color.FromArgb(24,33,47)};sectionPage.Controls.Add(row);var heading=LabelAt(row,title,20,16,row.Width-100,28,18,Color.White,true);heading.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;var copy=LabelAt(row,text,20,50,row.Width-100,48,14,Muted);copy.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;var toggle=new ToggleSwitch{Checked=value,Enabled=enabled,AccessibleName=title,Location=new Point(row.Width-64,24),Size=new Size(40,24),Anchor=AnchorStyles.Top|AnchorStyles.Right};toggle.Click+=(s,e)=>{toggle.Checked=!toggle.Checked;changed(toggle.Checked);toggle.Invalidate();};row.Controls.Add(toggle);y+=128;}
 void OpenReceivedFiles(){var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Velixa");System.IO.Directory.CreateDirectory(path);System.Diagnostics.Process.Start("explorer.exe", "\""+path+"\"");}
 internal bool ConfirmAction(string title,string message,string action){using(var f=new Form{Text=title,ClientSize=new Size(460,210),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false}){Visual.DarkTitle(f);LabelAt(f,title,24,20,412,34,23,Color.White,true);LabelAt(f,message,24,64,412,72,14,Muted);var cancel=Button("Cancel",()=>{f.DialogResult=DialogResult.Cancel;f.Close();});cancel.SetBounds(162,152,120,38);var confirm=Button(action,()=>{f.DialogResult=DialogResult.OK;f.Close();},true);confirm.SetBounds(294,152,142,38);f.Controls.AddRange(new Control[]{cancel,confirm});f.CancelButton=cancel;f.AcceptButton=cancel;return f.ShowDialog(this)==DialogResult.OK;}}

 void LayoutSection(){if(sectionPage==null||sectionPage.IsDisposed)return;sectionPage.SetBounds(deskCenter.Left,0,sectionHost.Width-deskCenter.Left,sectionHost.Height);foreach(Control c in sectionPage.Controls)if(c is SoftPanel)c.Width=Math.Max(260,sectionPage.ClientSize.Width-48);}
 void ShowSection(string name){activeSection=name;foreach(var n in navItems)n.Selected=n.Text==name;bool home=name=="My Desk";deskCenter.Visible=deskInspector.Visible=home;sectionPage.Visible=!home;if(home){desk.Focus();return;}foreach(Control c in sectionPage.Controls.Cast<Control>().ToArray())c.Dispose();LayoutSection();LabelAt(sectionPage,name,24,24,550,40,30,Color.White,true);int y=86;
 Action<string,string,Action,string> card=(title,description,action,caption)=>{var row=new SoftPanel{Location=new Point(24,y),Size=new Size(sectionPage.ClientSize.Width-48,148),FillColor=Color.FromArgb(24,33,47)};sectionPage.Controls.Add(row);var heading=LabelAt(row,title,20,16,row.Width-40,28,18,Color.White,true);heading.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;var copy=LabelAt(row,description,20,49,row.Width-40,action==null?78:42,14,Muted);copy.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;if(action!=null){var button=Button(caption,action);button.SetBounds(20,98,190,36);row.Controls.Add(button);}y+=164;};
 if(name=="Devices"){
 foreach(var d in session.Devices){var selected=d;card(d.name+(d.id==Network.LocalId?" · This PC":""),d.kind+"  ·  "+(d.Available?"Connected":d.sleeping?"Excluded from input":"Offline"),()=>{desk.Selected=selected;ShowSection("My Desk");RefreshInspector();desk.Invalidate();},"Show on desk");var row=sectionPage.Controls.OfType<SoftPanel>().Last();row.Height=286;y+=138;
 var size=Button("Screen size",()=>desk.EditSize(selected));size.SetBounds(20,144,140,36);size.Enabled=session.Connected;row.Controls.Add(size);
 var sleep=Button(selected.sleeping?"Include in sharing":"Exclude from sharing",()=>{session.SleepDevice(selected.id,!selected.sleeping);ShowSection("Devices");});sleep.SetBounds(20,186,220,36);sleep.Enabled=session.Connected;row.Controls.Add(sleep);
 if(d.id!=Network.LocalId){var forget=Button("Forget device",()=>{ForgetDevice(selected);if(session!=null)ShowSection("Devices");});forget.SetBounds(20,228,140,36);forget.Enabled=true;row.Controls.Add(forget);}
 }

 if(session.Devices.Count==1)card("Bring another screen to your desk","Use Add device on My Desk to pair a Windows PC or Android device over your local network.",()=>ShowSection("My Desk"),"Go to My Desk");
 }else if(name=="Help"){
 card("Controls","Drag screens on My Desk to match their positions. Ctrl + Alt + Backspace returns control here. Pause input and Disconnect are available on every screen.",null,null);
 card("Microphones","Choose an input from My Desk > Microphone. The selected PC starts capture automatically. Install a virtual audio cable once to use the audio in calling apps.",MicrophoneDialog,"Microphone output");
 card("Trackpad gestures","Updated Windows 11 PCs can forward scroll, pinch, and three- or four-finger gestures using the receiving PC’s Windows settings. Update Velixa on both PCs.",null,null);

 card("Velixa · "+new Version(Application.ProductVersion).ToString(3),"One input. Everywhere.\nShare your keyboard, mouse, clipboard, files and microphone across your local desk.",null,null);
 card("Open source · MIT license","Explore the source code, installation instructions and release downloads.",()=>System.Diagnostics.Process.Start("https://github.com/akasumitlamba/Velixa"),"Project & downloads");
 card("Need help?","Include your app version, device types and steps to reproduce when reporting a problem.",()=>System.Diagnostics.Process.Start("https://github.com/akasumitlamba/Velixa/issues"),"Report an issue");
 }else if(name=="Settings"){
 AddPreference(ref y,"Share clipboard","Share copied text and images with connected Windows PCs.",sharing!=null&&sharing.ClipboardEnabled,sharing!=null,value=>{sharing.ClipboardEnabled=value;Wire.SaveSecret("clipboard-enabled.bin",value?"true":"false");RefreshInspector();});
 AddPreference(ref y,"Receive files","Allow paired PCs to send files to Downloads / Velixa.",sharing!=null&&sharing.FilesEnabled,sharing!=null,value=>{sharing.FilesEnabled=value;Wire.SaveSecret("files-enabled.bin",value?"true":"false");});
 card("Received files","Open the folder containing files received from your paired PCs.",OpenReceivedFiles,"Open received files");
 card("Microphone output","Choose microphones from My Desk. Set up the app audio output here once.",MicrophoneDialog,"Microphone output");
 card("Screen-edge feedback","Preview the edge lights used when moving between connected screens.",()=>session.Input.PreviewEdges(),"Preview edge lights");
 card("Leave this desk","Disconnect this PC and return to setup. You can create or join a desk again.",()=>{if(ConfirmAction("Leave this desk?","Sharing on this PC will stop and you will return to setup.","Leave desk")){LeaveDesk();}},"Leave desk");

 }
 sectionPage.AutoScrollMinSize=new Size(0,y+8);LayoutSection();
 }
}
}
