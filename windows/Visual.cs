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
 public static void DarkTitle(Form f){try{int on=1;DwmSetWindowAttribute(f.Handle,20,ref on,4);}catch{}}
}
public class SoftButton:Button {
 public bool Primary;bool hover,pressed;
 public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.Transparent;ForeColor=Color.White;Cursor=Cursors.Hand;Font=Visual.Font(14,true);Height=44;Width=160;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-3,Height-3);var color=Primary?(pressed?Color.FromArgb(115,99,223):hover?Color.FromArgb(155,137,255):MainForm.Accent):(pressed?Color.FromArgb(44,48,63):hover?Color.FromArgb(49,53,68):Color.FromArgb(37,41,55));Visual.Fill(e.Graphics,r,11,color);if(!Primary)Visual.Stroke(e.Graphics,r,11,Color.FromArgb(58,62,77));Visual.Text(e.Graphics,Text,r,14,Enabled?Color.FromArgb(246,245,252):MainForm.Muted,true,StringAlignment.Center);if(Focused&&ShowFocusCues)Visual.Stroke(e.Graphics,new RectangleF(4,4,Width-9,Height-9),8,Color.FromArgb(192,180,255));}
}
public class SoftPanel:Panel {
 public Color FillColor=Color.FromArgb(27,30,41);public int Radius=16;
 public SoftPanel(){DoubleBuffered=true;BackColor=Color.Transparent;SetStyle(ControlStyles.ResizeRedraw,true);}
 protected override void OnPaintBackground(PaintEventArgs e){base.OnPaintBackground(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Visual.Fill(e.Graphics,new RectangleF(0,0,Width,Height),Radius,FillColor);Visual.Stroke(e.Graphics,new RectangleF(.5f,.5f,Width-1,Height-1),Radius,Color.FromArgb(43,47,61));}
}
}
