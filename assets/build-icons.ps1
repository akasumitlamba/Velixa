if ($PSVersionTable.PSVersion.Major -ge 6) { & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath; if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed' }; return }
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Globalization;
public static class VelixaIcon {
 public static Bitmap Render(string data,int size,bool tile){var bitmap=new Bitmap(size,size);using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.Clear(tile?Color.White:Color.Transparent);var tokens=Regex.Matches(data,@"[MLCZ]|-?\d+(?:\.\d+)?");int i=0;float x=0,y=0;using(var p=new GraphicsPath()){while(i<tokens.Count){string op=tokens[i++].Value;if(op=="Z"){p.CloseFigure();continue;}int count=op=="C"?6:2;var v=new float[count];for(int n=0;n<count;n++)v[n]=float.Parse(tokens[i++].Value,CultureInfo.InvariantCulture);if(op=="M"){p.StartFigure();x=v[0];y=v[1];}else if(op=="L"){p.AddLine(x,y,v[0],v[1]);x=v[0];y=v[1];}else{p.AddBezier(x,y,v[0],v[1],v[2],v[3],v[4],v[5]);x=v[4];y=v[5];}}var bounds=p.GetBounds();float margin=tile?.13f:.04f;float factor=size*(1-2*margin)/Math.Max(bounds.Width,bounds.Height);using(var matrix=new Matrix(factor,0,0,factor,(size-bounds.Width*factor)/2-bounds.X*factor,(size-bounds.Height*factor)/2-bounds.Y*factor))p.Transform(matrix);using(var brush=new LinearGradientBrush(new Rectangle(0,0,size,size),Color.FromArgb(38,175,232),Color.FromArgb(119,69,221),70))g.FillPath(brush,p);}}return bitmap;}
}
"@
[xml]$svg=Get-Content (Join-Path $PSScriptRoot '../assets/velixa-monochrome.svg')
$pathData=$svg.svg.path.d
$project=Split-Path $PSScriptRoot -Parent
foreach($entry in @(@('build/windows/velixa-logo.png',256,$false),@('android/res/drawable/logo_color.png',432,$false),@('android/res/mipmap-mdpi/ic_launcher.png',192,$true))){$img=[VelixaIcon]::Render($pathData,[int]$entry[1],[bool]$entry[2]);$img.Save((Join-Path $project $entry[0]),[Drawing.Imaging.ImageFormat]::Png);$img.Dispose()}
$sizes=@(16,20,24,32,40,48,64,128,256)
$frames=@()
foreach($size in $sizes){$img=[VelixaIcon]::Render($pathData,$size,$true);$stream=[IO.MemoryStream]::new();$img.Save($stream,[Drawing.Imaging.ImageFormat]::Png);$frames+=,@($stream.ToArray());$stream.Dispose();$img.Dispose()}
$file=[IO.File]::Create((Join-Path $project 'build/windows/velixa.ico'));$writer=[IO.BinaryWriter]::new($file)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count);$offset=6+16*$sizes.Count
for($i=0;$i -lt $sizes.Count;$i++){$size=$sizes[$i];$writer.Write([byte]($size%256));$writer.Write([byte]($size%256));$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frames[$i].Count);$writer.Write([uint32]$offset);$offset+=$frames[$i].Count}
foreach($frame in $frames){$writer.Write([byte[]]$frame)}
$writer.Dispose()
