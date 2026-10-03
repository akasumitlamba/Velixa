package com.velixa.app;
import android.app.*;
import android.content.*;
import android.content.pm.*;
import android.net.Uri;
import android.provider.Settings;
import java.io.*;
import java.net.*;
import java.security.*;
import java.util.*;
import org.json.*;

public final class Updater {
 static boolean checked,prompted,waitingPermission;static File ready;static String failure;
 static java.lang.ref.WeakReference<Activity> current=new java.lang.ref.WeakReference<>(null);
 static final String API="https://api.github.com/repos/akasumitlamba/Velixa/releases/latest";
 public static synchronized void launch(Activity a){current=new java.lang.ref.WeakReference<>(a);if(checked)return;checked=true;Context c=a.getApplicationContext();new Thread(()->{try{download(c);}catch(Exception e){android.util.Log.i("VelixaUpdate","Update unavailable",e);}Activity owner=current.get();if(owner!=null)owner.runOnUiThread(()->resume(owner));},"Velixa update").start();}
 static HttpURLConnection open(String address)throws Exception{URL url=new URL(address);for(int i=0;i<6;i++){if(!url.getProtocol().equals("https"))throw new IOException("HTTPS required");HttpURLConnection c=(HttpURLConnection)url.openConnection();c.setConnectTimeout(15000);c.setReadTimeout(20000);c.setInstanceFollowRedirects(false);c.setRequestProperty("User-Agent","Velixa-Updater");int status=c.getResponseCode();if(status>=300&&status<400){String location=c.getHeaderField("Location");c.disconnect();url=new URL(url,location);continue;}if(status!=200){c.disconnect();throw new IOException("HTTP "+status);}return c;}throw new IOException("Too many redirects");}
 static byte[] read(String url,int limit)throws Exception{HttpURLConnection c=open(url);try(InputStream in=c.getInputStream();ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] b=new byte[8192];int n;while((n=in.read(b))!=-1){if(out.size()+n>limit)throw new IOException("Response too large");out.write(b,0,n);}return out.toByteArray();}finally{c.disconnect();}}
 static boolean newer(String next,String installed){if(!next.matches("[0-9]+\\.[0-9]+\\.[0-9]+"))return false;try{String[] a=next.split("\\."),b=installed.split("\\.");for(int i=0;i<3;i++){int x=Integer.parseInt(a[i]),y=Integer.parseInt(b[i]);if(x!=y)return x>y;}return false;}catch(Exception e){return false;}}
 static JSONObject select(JSONObject release,String installed)throws Exception{if(release.optBoolean("draft")||release.optBoolean("prerelease"))return null;String tag=release.optString("tag_name"),version=tag.startsWith("v")?tag.substring(1):tag;if(!newer(version,installed))return null;String name="Velixa-"+version+"-Android.apk";JSONArray assets=release.optJSONArray("assets");if(assets==null)return null;for(int i=0;i<assets.length();i++){JSONObject a=assets.getJSONObject(i);if(a.optString("name").equals(name)&&a.optLong("size")>0&&a.optLong("size")<268435456&&a.optString("digest").matches("sha256:[a-fA-F0-9]{64}")&&a.optString("browser_download_url").equals("https://github.com/akasumitlamba/Velixa/releases/download/"+tag+"/"+name))return a;}return null;}
 static void download(Context c)throws Exception{
  PackageManager pm=c.getPackageManager();PackageInfo installed=pm.getPackageInfo(c.getPackageName(),PackageManager.GET_SIGNATURES);
  JSONObject asset=select(new JSONObject(new String(read(API,1048576),"UTF-8")),installed.versionName);if(asset==null)return;
  File dir=new File(c.getCacheDir(),"updates");dir.mkdirs();File part=new File(dir,"update.part"),apk=new File(dir,"update.apk");HttpURLConnection connection=open(asset.getString("browser_download_url"));
  try{MessageDigest digest=MessageDigest.getInstance("SHA-256");try(InputStream in=connection.getInputStream();OutputStream out=new FileOutputStream(part)){byte[] b=new byte[65536];long size=0;int n;while((n=in.read(b))!=-1){size+=n;if(size>asset.getLong("size"))throw new IOException("Unexpected size");out.write(b,0,n);digest.update(b,0,n);}if(size!=asset.getLong("size"))throw new IOException("Incomplete download");}
   StringBuilder hash=new StringBuilder();for(byte b:digest.digest())hash.append(String.format(Locale.ROOT,"%02x",b&255));if(!asset.getString("digest").equalsIgnoreCase("sha256:"+hash))throw new IOException("Checksum mismatch");
   PackageInfo incoming=pm.getPackageArchiveInfo(part.getPath(),PackageManager.GET_SIGNATURES);
   if(incoming==null||!c.getPackageName().equals(incoming.packageName)||incoming.versionCode<=installed.versionCode||!Arrays.equals(installed.signatures,incoming.signatures))throw new IOException("Update identity mismatch");
   if(apk.exists()&&!apk.delete())throw new IOException("Cannot replace update");if(!part.renameTo(apk))throw new IOException("Cannot save update");ready=apk;
  }finally{connection.disconnect();part.delete();}
 }
 public static void resume(Activity a){current=new java.lang.ref.WeakReference<>(a);if(a.isFinishing()||a.isDestroyed()||ready==null)return;if(waitingPermission){waitingPermission=false;if(a.getPackageManager().canRequestPackageInstalls())install(a);else{prompted=false;android.widget.Toast.makeText(a,"Update saved. Allow installation when you are ready.",1).show();}return;}if(prompted)return;prompted=true;new AlertDialog.Builder(a).setTitle("Velixa update ready").setMessage("A new stable update has downloaded. Install it now?").setNegativeButton("Later",(d,w)->{}).setPositiveButton("Install",(d,w)->install(a)).show();}
 static void install(Activity a){try{if(!a.getPackageManager().canRequestPackageInstalls()){waitingPermission=true;a.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,Uri.parse("package:"+a.getPackageName())));return;}Uri uri=Uri.parse("content://com.velixa.app.updates/update.apk");a.startActivity(new Intent(Intent.ACTION_INSTALL_PACKAGE).setData(uri).addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION));}catch(Exception e){prompted=false;android.widget.Toast.makeText(a,"Could not open installer. Try again when Velixa next launches.",1).show();}}
}
