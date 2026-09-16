using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace Velixa {
public static class Visual {
 public static Font Font(float size,bool bold=false){return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Max(1,Math.Min(radius*2,Math.Min(r.Width,r.Height)));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static void Fill(Graphics g,RectangleF r,float radius,Color c){using(var p=Round(r,radius))using(var b=new SolidBrush(c))g.FillPath(b,p);}
 public static void Stroke(Graphics g,RectangleF r,float radius,Color c,float width=1){using(var p=Round(r,radius))using(var pen=new Pen(c,width))g.DrawPath(pen,p);}
 public static void Text(Graphics g,string text,RectangleF r,float size,Color color,bool bold=false,StringAlignment align=StringAlignment.Near){using(var f=Font(size,bold))using(var b=new SolidBrush(color))using(var sf=new StringFormat{Alignment=align,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(text,f,b,r,sf);}
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
 public static void DarkTitle(Form f){f.Shown+=(s,e)=>ThemeDialog(f);try{int on=1;DwmSetWindowAttribute(f.Handle,20,ref on,4);}catch{}}
 static void ThemeDialog(Control parent){foreach(Control c in parent.Controls){if(c is TextBox){c.BackColor=Color.FromArgb(24,33,47);c.ForeColor=Color.White;((TextBox)c).BorderStyle=BorderStyle.FixedSingle;}if(c is CheckBox){c.Font=Font(14);c.ForeColor=Color.FromArgb(228,234,245);((CheckBox)c).FlatStyle=FlatStyle.Flat;((CheckBox)c).FlatAppearance.CheckedBackColor=MainForm.Accent;}if(c is TrackBar)c.BackColor=MainForm.Bg;if(c is LinkLabel)((LinkLabel)c).LinkColor=MainForm.Accent;ThemeDialog(c);}}

}
public class SoftButton:Button {
 public string IconName;public bool Primary;bool hover,pressed;
 public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.Transparent;ForeColor=Color.White;Cursor=Cursors.Hand;Font=Visual.Font(14,true);Height=44;Width=160;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color background=MainForm.Bg;for(Control c=Parent;c!=null;c=c.Parent){if(c is SoftPanel){background=((SoftPanel)c).FillColor;break;}if(c.BackColor.A==255){background=c.BackColor;break;}}e.Graphics.Clear(background);var r=new RectangleF(0,0,Width-1,Height-1);var color=Primary?(pressed?Color.FromArgb(115,99,223):hover?Color.FromArgb(155,137,255):MainForm.Accent):(pressed?Color.FromArgb(44,48,63):hover?Color.FromArgb(49,53,68):Color.FromArgb(37,41,55));Visual.Fill(e.Graphics,r,11,color);if(!Primary)Visual.Stroke(e.Graphics,r,11,Color.FromArgb(47,59,82));if(IconName!=null)UiIcons.Draw(e.Graphics,IconName,new RectangleF((Width-20)/2f,(Height-20)/2f,20,20),MainForm.Muted);Visual.Text(e.Graphics,Text,r,14,Enabled?Color.FromArgb(246,245,252):MainForm.Muted,true,StringAlignment.Center);if(Focused&&ShowFocusCues)Visual.Stroke(e.Graphics,new RectangleF(3,3,Width-7,Height-7),8,Color.FromArgb(192,180,255));}
}
public class DarkComboBox:ComboBox {
 public DarkComboBox(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=28;FlatStyle=FlatStyle.Flat;BackColor=MainForm.PanelColor;ForeColor=Color.White;DropDownStyle=ComboBoxStyle.DropDownList;Font=Visual.Font(14);DropDownWidth=650;}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0xF&&IsHandleCreated){using(var g=Graphics.FromHwnd(Handle)){var r=new Rectangle(Math.Max(0,Width-24),1,23,Height-2);using(var b=new SolidBrush(MainForm.PanelColor))g.FillRectangle(b,r);using(var pen=new Pen(Color.FromArgb(47,59,82)))g.DrawRectangle(pen,0,0,Width-1,Height-1);g.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(Enabled?Color.White:MainForm.Muted,1.4f)){float x=Width-13,y=Height/2f;g.DrawLines(pen,new[]{new PointF(x-4,y-2),new PointF(x,y+2),new PointF(x+4,y-2)});}}}}
 protected override void OnDrawItem(DrawItemEventArgs e){Color bg=(e.State&DrawItemState.Selected)!=0?Color.FromArgb(49,53,68):MainForm.PanelColor;using(var brush=new SolidBrush(bg))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index>=0&&e.Index<Items.Count)TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-16,e.Bounds.Height),Enabled?Color.White:MainForm.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);}
}
public class SoftPanel:Panel {
 public Color FillColor=Color.FromArgb(27,30,41);public int Radius=16;
 public SoftPanel(){DoubleBuffered=true;BackColor=Color.Transparent;SetStyle(ControlStyles.ResizeRedraw,true);}
 protected override void OnPaintBackground(PaintEventArgs e){base.OnPaintBackground(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Visual.Fill(e.Graphics,new RectangleF(0,0,Width,Height),Radius,FillColor);Visual.Stroke(e.Graphics,new RectangleF(.5f,.5f,Width-1,Height-1),Radius,Color.FromArgb(43,47,61));}
}
public class DarkMenuRenderer:ToolStripProfessionalRenderer {
 public DarkMenuRenderer():base(new DarkMenuColors()){}
 protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){var r=new Rectangle(4,1,e.Item.Width-8,e.Item.Height-2);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(e.Item.Selected&&e.Item.Enabled)Visual.Fill(e.Graphics,new RectangleF(r.X,r.Y,r.Width,r.Height),6,Color.FromArgb(38,48,72));else using(var b=new SolidBrush(MainForm.PanelColor))e.Graphics.FillRectangle(b,r);}
 protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){var r=new Rectangle(e.ImageRectangle.X+2,e.ImageRectangle.Y+2,e.ImageRectangle.Width-4,e.ImageRectangle.Height-4);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Visual.Fill(e.Graphics,new RectangleF(r.X,r.Y,r.Width,r.Height),4,MainForm.Accent);using(var pen=new Pen(Color.White,2)){e.Graphics.DrawLine(pen,r.X+3,r.Y+r.Height/2,r.X+r.Width/2-1,r.Bottom-4);e.Graphics.DrawLine(pen,r.X+r.Width/2-1,r.Bottom-4,r.Right-3,r.Y+3);}}
 protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(Color.FromArgb(43,47,61)))e.Graphics.DrawLine(pen,24,e.Item.Height/2,e.Item.Width-24,e.Item.Height/2);}
 protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(MainForm.PanelColor);var r=new RectangleF(0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Visual.Fill(e.Graphics,r,10,MainForm.PanelColor);Visual.Stroke(e.Graphics,r,10,Color.FromArgb(47,59,82));}
 protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){}
 protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?Color.FromArgb(230,232,240):Color.FromArgb(100,105,122);base.OnRenderItemText(e);}
 class DarkMenuColors:ProfessionalColorTable{public override Color MenuBorder{get{return Color.FromArgb(47,59,82);}}public override Color MenuItemBorder{get{return Color.Transparent;}}public override Color MenuItemSelected{get{return Color.FromArgb(38,48,72);}}public override Color MenuStripGradientBegin{get{return MainForm.PanelColor;}}public override Color MenuStripGradientEnd{get{return MainForm.PanelColor;}}public override Color MenuItemSelectedGradientBegin{get{return Color.FromArgb(38,48,72);}}public override Color MenuItemSelectedGradientEnd{get{return Color.FromArgb(38,48,72);}}public override Color MenuItemPressedGradientBegin{get{return Color.FromArgb(45,56,82);}}public override Color MenuItemPressedGradientEnd{get{return Color.FromArgb(45,56,82);}}public override Color ImageMarginGradientBegin{get{return MainForm.PanelColor;}}public override Color ImageMarginGradientMiddle{get{return MainForm.PanelColor;}}public override Color ImageMarginGradientEnd{get{return MainForm.PanelColor;}}public override Color SeparatorDark{get{return Color.FromArgb(43,47,61);}}public override Color SeparatorLight{get{return Color.Transparent;}}public override Color ToolStripDropDownBackground{get{return MainForm.PanelColor;}}}
}
public static class DarkMenu {
 static readonly DarkMenuRenderer renderer=new DarkMenuRenderer();
 public static ContextMenuStrip Create(){var menu=new ContextMenuStrip{MaximumSize=new Size(680,0),BackColor=MainForm.PanelColor,ForeColor=Color.FromArgb(230,232,240),Font=Visual.Font(14),ShowImageMargin=true,Renderer=renderer,Padding=new Padding(4,8,4,8)};menu.ItemAdded+=(s,e)=>{e.Item.Padding=new Padding(12,8,12,8);};return menu;}
}
}
