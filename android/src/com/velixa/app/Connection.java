package com.velixa.app;
import org.json.JSONObject;
import java.net.*;
import java.io.*;
import java.security.*;
import java.security.cert.X509Certificate;
import javax.net.ssl.*;
import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.util.*;
import android.content.Context;
import android.os.Build;
public final class Connection {
 public interface Listener {void status(String s);void message(JSONObject m);int width();int height();}
 volatile boolean stopped=false;volatile Socket socket;volatile BufferedWriter writer;final Listener listener;String host,token,fingerprint,hostId;final String id;final Context context;boolean initial;
 public Connection(Context context,String host,String secret,String id,Listener listener){this.context=context;this.host=host;this.id=id;this.listener=listener;try{JSONObject data=new JSONObject(secret);token=data.getString("token");fingerprint=data.getString("fp");hostId=data.getString("id");initial=data.optBoolean("initial");}catch(Exception e){token="";fingerprint="";hostId="";}}
 public static String clean(String s){return s.replace("-","").replace(" ","").trim().toLowerCase(Locale.ROOT);}
 static String hex(byte[] bytes){StringBuilder s=new StringBuilder();for(byte b:bytes)s.append(String.format(Locale.ROOT,"%02x",b&255));return s.toString();}
 static String mac(String key,String value)throws Exception{Mac m=Mac.getInstance("HmacSHA256");m.init(new SecretKeySpec(key.getBytes("UTF-8"),"HmacSHA256"));return hex(m.doFinal(value.getBytes("UTF-8")));}
 static boolean equal(String a,String b)throws Exception{return MessageDigest.isEqual(a.getBytes("UTF-8"),b.getBytes("UTF-8"));}
 static String line(BufferedReader r)throws Exception{StringBuilder s=new StringBuilder();for(int c;(c=r.read())!=-1;){if(c==10)return s.toString();if(c!=13)s.append((char)c);if(s.length()>16384)throw new IOException("Message too large");}throw new EOFException();}
 public void start(){new Thread(()->{if(token.length()!=64||fingerprint.length()!=64){listener.status("Scan a new QR code to pair");return;}while(!stopped){try{
  listener.status("Looking for your desk…");if(!initial){for(String[] d:discover())if(d[2].equals(hostId)){host=d[0];break;}}
  SSLContext ctx=SSLContext.getInstance("TLSv1.2");ctx.init(null,new TrustManager[]{new X509TrustManager(){public X509Certificate[] getAcceptedIssuers(){return new X509Certificate[0];}public void checkClientTrusted(X509Certificate[] c,String a)throws java.security.cert.CertificateException{throw new java.security.cert.CertificateException();}public void checkServerTrusted(X509Certificate[] c,String a)throws java.security.cert.CertificateException{try{if(c.length<1||!equal(fingerprint,hex(MessageDigest.getInstance("SHA-256").digest(c[0].getEncoded()))))throw new java.security.cert.CertificateException("This is not your paired PC");}catch(Exception e){throw new java.security.cert.CertificateException(e);}}}},new SecureRandom());
  SSLSocket s=(SSLSocket)ctx.getSocketFactory().createSocket();socket=s;s.connect(new InetSocketAddress(host,37128),4000);s.setSoTimeout(8000);s.setTcpNoDelay(true);s.startHandshake();BufferedReader r=new BufferedReader(new InputStreamReader(s.getInputStream(),"UTF-8"));BufferedWriter w=new BufferedWriter(new OutputStreamWriter(s.getOutputStream(),"UTF-8"));JSONObject hello=new JSONObject(line(r));if(hello.optInt("v")!=2||!hello.optString("id").equals(hostId))throw new IOException("Update or re-pair your PC");byte[] random=new byte[32];new SecureRandom().nextBytes(random);String cn=hex(random),basis="Velixa-v2|"+hostId+"|"+id+"|"+fingerprint+"|"+hello.getString("nonce")+"|"+cn;
  write(w,new JSONObject().put("mode",initial?"qr":"resume").put("proof",mac(token,basis+"|client")).put("cn",cn).put("name",Build.MODEL).put("kind","Android").put("id",id).put("w",listener.width()).put("h",listener.height()));JSONObject ready=new JSONObject(line(r));String next=initial?ready.optString("token"):token;if(!ready.optString("t").equals("ready")||next.length()!=64||!equal(ready.optString("proof"),mac(next,basis+"|server")))throw new IOException("Pairing expired. Scan a new code.");token=next;initial=false;SecureStore.put(context,new JSONObject().put("token",token).put("fp",fingerprint).put("id",hostId).toString());context.getSharedPreferences("velixa",0).edit().putString("host",host).apply();writer=w;listener.status("Connected · ready for your pointer");int width=listener.width(),height=listener.height();
  while(!stopped){JSONObject m=new JSONObject(line(r));if(m.optString("t").equals("ping")){send(new JSONObject().put("t","pong"));int nw=listener.width(),nh=listener.height();if(nw!=width||nh!=height){width=nw;height=nh;send(new JSONObject().put("t","size").put("w",width).put("h",height));}}else listener.message(m);}
 }catch(Exception e){if(!stopped)listener.status(initial?"Pairing expired or unavailable. Scan a new code.":"Desk is offline · waiting nearby");if(initial){context.getSharedPreferences("velixa",0).edit().putBoolean("connected",false).apply();break;}}finally{writer=null;try{if(socket!=null)socket.close();listener.message(new JSONObject().put("t","offline"));}catch(Exception ignored){}}try{Thread.sleep(2500);}catch(InterruptedException ignored){}}},"Velixa local connection").start();}
 static synchronized void write(BufferedWriter w,JSONObject m)throws IOException{w.write(m.toString());w.write('\n');w.flush();}
 public void send(JSONObject m){BufferedWriter w=writer;if(w==null)return;try{write(w,m);}catch(Exception e){try{socket.close();}catch(Exception ignored){}}}
 public void stop(){stopped=true;try{if(socket!=null)socket.close();}catch(Exception ignored){}}
 public static List<String[]> discover(){List<String[]> result=new ArrayList<>();try(DatagramSocket s=new DatagramSocket()){s.setBroadcast(true);s.setSoTimeout(300);byte[] b="VELIXA_DISCOVER_2".getBytes("UTF-8");s.send(new DatagramPacket(b,b.length,InetAddress.getByName("255.255.255.255"),37129));long end=System.currentTimeMillis()+1000;Set<String> seen=new HashSet<>();while(System.currentTimeMillis()<end){try{DatagramPacket p=new DatagramPacket(new byte[512],512);s.receive(p);String[] v=new String(p.getData(),0,p.getLength(),"UTF-8").split("\\|");if(v.length==3&&v[0].equals("VELIXA_2")&&seen.add(v[2]))result.add(new String[]{p.getAddress().getHostAddress(),v[1],v[2]});}catch(SocketTimeoutException ignored){}}}catch(Exception ignored){}return result;}
}
