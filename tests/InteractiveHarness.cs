using System;
using Velixa;
public class InteractiveHarness {
 public static void Main(string[] args){using(var n=new Network()){n.Code=args[0];n.Joined=p=>Console.WriteLine("JOINED");n.Host();Console.WriteLine("READY");string s;while((s=Console.ReadLine())!=null){try{var message=Wire.Parse(s);foreach(var p in n.Snapshot())p.Send(message);Console.WriteLine("SENT");}catch{Console.WriteLine("ERROR");}}}
}

}
