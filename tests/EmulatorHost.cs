using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Velixa;
class EmulatorHost {
 [STAThread]static void Main(){Application.EnableVisualStyles();using(var window=new Form())using(var network=new Network()){var handle=window.Handle;var session=new DeskSession(network,new Receiver(),a=>window.BeginInvoke(a));network.Host();session.Host();session.Input.Enabled=false;network.OpenPairing();Action qr=()=>{File.WriteAllText("build/test-qr.txt",network.Qr("10.0.2.2"));using(var gen=new QRCoder.QRCodeGenerator())using(var data=gen.CreateQrCode(network.Qr("10.0.2.2"),QRCoder.QRCodeGenerator.ECCLevel.M))using(var code=new QRCoder.QRCode(data))using(var bmp=code.GetGraphic(8))bmp.Save("build/test-qr.png");};qr();File.WriteAllText("build/emulator-host.ready","ready");int read=0;var timer=new System.Windows.Forms.Timer{Interval=150};timer.Tick+=(s,e)=>{session.Input.Enabled=false;try{if(File.Exists("build/emulator-commands.txt")){var lines=File.ReadAllLines("build/emulator-commands.txt");while(read<lines.Length){string line=lines[read++];if(line=="qr"){network.OpenPairing();qr();}else if(line=="quit"){Application.ExitThread();return;}else foreach(var p in network.Snapshot())p.Send(Wire.Parse(line));}}}catch(Exception ex){File.AppendAllText("build/emulator-host-errors.txt",ex.Message+"\n");}};timer.Start();Application.Run();session.Dispose();}}
}
