using System;
using System.IO;
using System.Text;
namespace Velixa {public static class SelfTest {
 public static void Run(){int n=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);n++;};
 check(Wire.Clean("ABCD-1234 ef56-7890")=="abcd1234ef567890","pairing normalization");
 check(Wire.Mac("key","The quick brown fox jumps over the lazy dog")=="f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8","HMAC known vector");
 check(!Wire.Equal("abc","abd"),"wrong proof rejected");check(!Wire.Equal("abc","ab"),"short proof rejected");
 var m=Wire.Parse(Wire.Json(new{t="key",text="नमस्ते",down=true}));check(Wire.S(m,"text")=="नमस्ते","unicode wire");check(Wire.S(m,"down")=="True","boolean wire");
 bool rejected=false;try{using(var r=new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(new string('x',17000)+"\n"))))Wire.ReadLine(r);}catch(IOException){rejected=true;}check(rejected,"oversize frame rejected");
 using(var cert=Wire.Certificate())check(cert.HasPrivateKey,"TLS identity");
 Wire.SaveSecret("test.bin","sample");check(Wire.LoadSecret("test.bin","")=="sample","protected settings");File.Delete(Path.Combine(Wire.DataDir,"test.bin"));
 File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),n+" checks passed");
 }
}}
