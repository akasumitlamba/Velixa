using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace Velixa {
public sealed class UpdateAsset { public string name,browser_download_url,digest; public long size; }
public sealed class UpdateRelease { public string tag_name; public bool draft,prerelease; public UpdateAsset[] assets; }
public static class Updater {
 public const string Api="https://api.github.com/repos/akasumitlamba/Velixa/releases/latest";
 public static UpdateAsset Select(UpdateRelease release,Version current){
  if(release==null||release.draft||release.prerelease)return null;
  string version=(release.tag_name??"").TrimStart('v');Version next;
  if(!Regex.IsMatch(version,@"^\d+\.\d+\.\d+$")||!Version.TryParse(version,out next)||next<=new Version(current.Major,current.Minor,current.Build))return null;
  return (release.assets??new UpdateAsset[0]).FirstOrDefault(a=>a.name=="Velixa-"+version+"-Windows-Setup.exe"&&a.size>0&&a.size<268435456&&Regex.IsMatch(a.digest??"",@"^sha256:[a-fA-F0-9]{64}$")&&a.browser_download_url=="https://github.com/akasumitlamba/Velixa/releases/download/"+release.tag_name+"/"+a.name);
 }
 sealed class Client:WebClient { protected override WebRequest GetWebRequest(Uri uri){var r=base.GetWebRequest(uri);r.Timeout=20000;return r;} }
 public static string Download(){
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  using(var client=new Client()){
   client.Headers["User-Agent"]="Velixa-Updater";
   var release=new JavaScriptSerializer().Deserialize<UpdateRelease>(client.DownloadString(Api));
   var asset=Select(release,typeof(Updater).Assembly.GetName().Version);if(asset==null)return null;
   string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Velixa","Updates");Directory.CreateDirectory(dir);
   string path=Path.Combine(dir,asset.name),part=path+".part";
   try{using(var input=client.OpenRead(asset.browser_download_url))using(var output=File.Create(part)){var buffer=new byte[65536];long total=0;int count;while((count=input.Read(buffer,0,buffer.Length))>0){total+=count;if(total>asset.size)throw new IOException("Unexpected update size");output.Write(buffer,0,count);}if(total!=asset.size)throw new IOException("Incomplete update");}
    using(var stream=File.OpenRead(part))using(var sha=SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");if(!String.Equals(hash,asset.digest.Substring(7),StringComparison.OrdinalIgnoreCase))throw new IOException("Update checksum mismatch");}
    if(File.Exists(path))File.Delete(path);File.Move(part,path);return path;
   }finally{if(File.Exists(part))File.Delete(part);}
  }
 }
}
public partial class MainForm {
 internal async void CheckForUpdate(){
  try{string path=await Task.Run(()=>Updater.Download());if(path==null||IsDisposed)return;
   if(MessageBox.Show(this,"A new stable Velixa update has downloaded. Install it now? Sharing will stop while setup runs.","Velixa update",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
   System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});closing=true;Close();
  }catch(Exception e){System.Diagnostics.Trace.WriteLine("Update check: "+e.Message);}
 }
}
}
