package com.velixa.app;
import android.app.*;
import android.content.*;
import android.media.*;
import android.os.*;
import org.json.*;
import java.util.*;
import java.util.concurrent.*;

/** User-enabled, visible microphone sender. Never receives audio on Android. */
public final class MicrophoneService extends Service {
 public static volatile MicrophoneService instance;
 private volatile String stream="";private volatile boolean running;private Thread worker;
 private final Set<String> accepted=ConcurrentHashMap.newKeySet();private final Set<String> targets=ConcurrentHashMap.newKeySet();
 static String fresh(){return UUID.randomUUID().toString().replace("-","");}
 static AudioDeviceInfo[] devices(Context c){return ((AudioManager)c.getSystemService(AUDIO_SERVICE)).getDevices(AudioManager.GET_DEVICES_INPUTS);}
 static String label(AudioDeviceInfo d){String type=d.getType()==AudioDeviceInfo.TYPE_BUILTIN_MIC?"System microphone":d.getType()==AudioDeviceInfo.TYPE_USB_DEVICE||d.getType()==AudioDeviceInfo.TYPE_USB_HEADSET?"USB microphone":"External microphone";return type+" · "+d.getProductName();}
 static void emit(String to,JSONObject body){InputService service=InputService.instance;if(service!=null)service.sendShare(to,body);}
 static JSONObject message(String kind,String id){try{return new JSONObject().put("kind",kind).put("id",id);}catch(Exception e){throw new IllegalStateException(e);}}
 public static void list(Context c,String to){try{JSONArray mics=new JSONArray();for(AudioDeviceInfo d:devices(c))mics.put(new JSONObject().put("id",d.getId()).put("name",label(d)));emit(to,message("mic-list",fresh()).put("peerName",Build.MODEL).put("mics",mics));}catch(Exception ignored){}}
 public static void handle(Context c,String from,JSONObject body){String kind=body.optString("kind"),id=body.optString("id");if(!id.matches("[a-fA-F0-9]{32}"))return;
  if(kind.equals("mic-list-request")){list(c,from);return;}
  if(kind.equals("mic-request")){if(instance==null){try{emit(from,message("mic-error",id).put("message","Open Velixa on Android and enable microphone sharing first"));}catch(Exception ignored){}}else instance.begin(from,body.optInt("devId",-1));return;}
  MicrophoneService self=instance;if(self==null)return;
  if(kind.equals("mic-accept")&&id.equals(self.stream)&&self.targets.contains(from)&&body.optBoolean("ok"))self.accepted.add(from);
  if(kind.equals("mic-stop")&&id.equals(self.stream)){self.targets.remove(from);self.accepted.remove(from);if(self.targets.isEmpty())self.stopStream();}
 }
 @Override public void onCreate(){super.onCreate();instance=this;NotificationManager nm=(NotificationManager)getSystemService(NOTIFICATION_SERVICE);nm.createNotificationChannel(new NotificationChannel("microphone","Microphone sharing",NotificationManager.IMPORTANCE_LOW));}
 @Override public int onStartCommand(Intent intent,int flags,int startId){if(intent!=null&&"stop".equals(intent.getAction())){stopSelf();return START_NOT_STICKY;}
  PendingIntent stop=PendingIntent.getService(this,0,new Intent(this,MicrophoneService.class).setAction("stop"),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  Notification n=new Notification.Builder(this,"microphone").setSmallIcon(android.R.drawable.ic_btn_speak_now).setContentTitle("Velixa microphone sharing enabled").setContentText("Paired PCs can select this phone’s microphone. Tap Stop to disable.").setOngoing(true).addAction(new Notification.Action.Builder(null,"Stop",stop).build()).build();
  startForeground(27,n);return START_NOT_STICKY;
 }
 synchronized void begin(String requester,int deviceId){
  stopStream();if(worker!=null&&worker.isAlive()){try{emit(requester,message("mic-error",fresh()).put("message","Microphone is stopping. Select it again shortly."));}catch(Exception ignored){}return;}
  AudioDeviceInfo selected=null;for(AudioDeviceInfo d:devices(this))if(d.getId()==deviceId)selected=d;if(selected==null)return;
  stream=fresh();final String id=stream;final AudioDeviceInfo device=selected;targets.add(requester);
  JSONArray peers=InputService.deskDevices;if(peers!=null)for(int i=0;i<peers.length();i++){JSONObject d=peers.optJSONObject(i);if(d!=null&&d.optString("kind").equals("Windows")&&d.optBoolean("online")&&!d.optBoolean("sleeping"))targets.add(d.optString("id"));}
  try{for(String peer:targets)emit(peer,message("mic-begin",id).put("rate",48000).put("channels",1).put("bits",16));}catch(Exception ignored){}
  running=true;worker=new Thread(()->{AudioRecord record=null;try{
   long until=SystemClock.elapsedRealtime()+10000;while(running&&accepted.isEmpty()&&SystemClock.elapsedRealtime()<until)Thread.sleep(20);if(!running||accepted.isEmpty())return;
   int size=Math.max(15360,AudioRecord.getMinBufferSize(48000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT));
   record=new AudioRecord(MediaRecorder.AudioSource.MIC,48000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT,size);
   if(record.getState()!=AudioRecord.STATE_INITIALIZED||!record.setPreferredDevice(device))throw new IllegalStateException("Microphone unavailable");record.startRecording();byte[] buffer=new byte[3840];
   while(running&&id.equals(stream)&&InputService.online){int count=record.read(buffer,0,buffer.length);if(count<0)throw new IllegalStateException("Microphone disconnected");if(count==0)continue;String data=android.util.Base64.encodeToString(buffer,0,count,android.util.Base64.NO_WRAP);for(String peer:accepted)emit(peer,message("mic-data",id).put("data",data));}
  }catch(Exception e){try{emit(requester,message("mic-error",id).put("message","Android microphone unavailable; check its audio permission or selected input"));}catch(Exception ignored){}}
  finally{if(record!=null){try{record.stop();}catch(Exception ignored){}record.release();}for(String peer:targets)emit(peer,message("mic-stop",id));running=false;accepted.clear();targets.clear();}},"Velixa microphone");worker.start();
 }
 synchronized void stopStream(){running=false;for(String peer:targets)emit(peer,message("mic-stop",stream));accepted.clear();targets.clear();}
 @Override public void onDestroy(){stopStream();instance=null;super.onDestroy();}
 @Override public IBinder onBind(Intent intent){return null;}
}
