using System;
using System.IO;
using System.Threading;
using Velixa;
public class NetworkHarness {
 public static void Main(string[] args){using(var n=new Network()) {n.Code=args[0];n.Joined=p=>{p.Send(new{t="enter",edge="left",x=.25,y=.5});p.Send(new{t="key",vk=65,scan=30,down=true,text="नमस्ते",ctrl=false,shift=false,alt=false});};n.Host();Console.WriteLine("READY");Console.Out.Flush();Thread.Sleep(30000);}}
}
