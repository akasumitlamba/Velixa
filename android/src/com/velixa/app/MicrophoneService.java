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
 private static final class Stream {
  final String peer,id;final AudioDeviceInfo device;volatile boolean running=true,accepted;Thread worker;
  Stream(String peer,String id,AudioDeviceInfo device){this.peer=peer;this.id=id;this.device=device;}
 }
 private final ConcurrentHashMap<String,Stream> streams=new ConcurrentHashMap<>();
 static String fresh(){return UUID.randomUUID().toString().replace("-","");}
 static AudioDeviceInfo[] devices(Context c){
  AudioDeviceInfo[] inputs=((AudioManager)c.getSystemService(AUDIO_SERVICE)).getDevices(AudioManager.GET_DEVICES_INPUTS);
  List<MicrophoneRoutes.Route> routes=new ArrayList<>();Map<Integer,AudioDeviceInfo> byId=new HashMap<>();
  for(AudioDeviceInfo d:inputs){routes.add(new MicrophoneRoutes.Route(d.getId(),d.getType(),Build.VERSION.SDK_INT>=28?d.getAddress():"",String.valueOf(d.getProductName()),d.isSource()));byId.put(d.getId(),d);}
  List<AudioDeviceInfo> selected=new ArrayList<>();for(MicrophoneRoutes.Route route:MicrophoneRoutes.select(routes))selected.add(byId.get(route.id));return selected.toArray(new AudioDeviceInfo[0]);
 }
 static String label(AudioDeviceInfo d){return MicrophoneRoutes.label(d.getType(),String.valueOf(d.getProductName()),Build.MODEL);}
 static void emit(String to,JSONObject body){InputService service=InputService.instance;if(service!=null)service.sendShare(to,body);}
 static JSONObject message(String kind,String id){try{return new JSONObject().put("kind",kind).put("id",id);}catch(Exception e){throw new IllegalStateException(e);}}
 public static void list(Context c,String to){try{JSONArray mics=new JSONArray();for(AudioDeviceInfo d:devices(c))mics.put(new JSONObject().put("id",d.getId()).put("name",label(d)));emit(to,message("mic-list",fresh()).put("peerName",Build.MODEL).put("mics",mics));}catch(Exception ignored){}}
 public static void handle(Context c,String from,JSONObject body){String kind=body.optString("kind"),id=body.optString("id");if(!id.matches("[a-fA-F0-9]{32}"))return;
  if(kind.equals("mic-list-request")){list(c,from);return;}
  if(kind.equals("mic-request")){if(instance==null){try{emit(from,message("mic-error",id).put("message","Open Velixa on Android and allow PCs to use its microphone first"));}catch(Exception ignored){}}else instance.begin(from,id,body.optInt("devId",-1),body.optString("micName",""));return;}
  MicrophoneService self=instance;if(self==null)return;Stream stream=self.streams.get(from);if(stream==null||!id.equals(stream.id))return;
  if(kind.equals("mic-accept")){if(body.optBoolean("ok"))stream.accepted=true;else self.stop(stream);}
  if(kind.equals("mic-stop"))self.stop(stream);
 }
 @Override public void onCreate(){super.onCreate();instance=this;NotificationManager nm=(NotificationManager)getSystemService(NOTIFICATION_SERVICE);nm.createNotificationChannel(new NotificationChannel("microphone","Microphone sharing",NotificationManager.IMPORTANCE_LOW));}
 @Override public int onStartCommand(Intent intent,int flags,int startId){if(intent!=null&&"stop".equals(intent.getAction())){stopSelf();return START_NOT_STICKY;}
  PendingIntent stop=PendingIntent.getService(this,0,new Intent(this,MicrophoneService.class).setAction("stop"),PendingIntent.FLAG_IMMUTABLE|PendingIntent.FLAG_UPDATE_CURRENT);
  Notification n=new Notification.Builder(this,"microphone").setSmallIcon(android.R.drawable.ic_btn_speak_now).setContentTitle("Velixa microphones available").setContentText("Choose this phone’s microphone on a paired PC. Stop disables access.").setOngoing(true).addAction(new Notification.Action.Builder(null,"Stop",stop).build()).build();
  startForeground(27,n);return START_NOT_STICKY;
 }
 synchronized void begin(String requester,String requestId,int deviceId,String expectedName){
  AudioDeviceInfo selected=null;for(AudioDeviceInfo d:devices(this))if(d.getId()==deviceId&&(expectedName.isEmpty()||expectedName.equals(label(d))))selected=d;
  if(selected==null){error(requester,requestId,"Microphone list changed. Close and reopen the microphone menu.");return;}
  Stream previous=streams.get(requester);if(previous!=null)stop(previous);
  final Stream stream=new Stream(requester,requestId,selected);streams.put(requester,stream);
  try{emit(requester,message("mic-begin",requestId).put("request",requestId).put("rate",48000).put("channels",1).put("bits",16));}catch(Exception e){stop(stream);return;}
  stream.worker=new Thread(()->capture(stream,previous),"Velixa microphone");stream.worker.start();
 }
 static void error(String peer,String id,String text){try{emit(peer,message("mic-error",id).put("message",text));}catch(Exception ignored){}}
 void capture(Stream stream,Stream previous){AudioRecord record=null;try{
   // Complete this receiver's old capture before opening its replacement.
   if(previous!=null&&previous.worker!=null){previous.worker.join(1500);if(previous.worker.isAlive())throw new IllegalStateException("Previous microphone is still stopping");}
   long until=SystemClock.elapsedRealtime()+10000;while(stream.running&&!stream.accepted&&SystemClock.elapsedRealtime()<until)Thread.sleep(20);if(!stream.running||!stream.accepted)return;
   int minimum=AudioRecord.getMinBufferSize(48000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT);if(minimum<=0)throw new IllegalStateException("PCM format unavailable");
   record=new AudioRecord(MediaRecorder.AudioSource.MIC,48000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT,Math.max(15360,minimum));
   if(record.getState()!=AudioRecord.STATE_INITIALIZED||!record.setPreferredDevice(stream.device))throw new IllegalStateException("Microphone unavailable");record.startRecording();byte[] buffer=new byte[3840];
   while(stream.running&&InputService.online){int count=record.read(buffer,0,buffer.length,AudioRecord.READ_NON_BLOCKING);if(count<0)throw new IllegalStateException("Microphone disconnected");if(count==0){Thread.sleep(10);continue;}String data=android.util.Base64.encodeToString(buffer,0,count,android.util.Base64.NO_WRAP);if(stream.running)emit(stream.peer,message("mic-data",stream.id).put("data",data));}
  }catch(Exception e){if(stream.running)error(stream.peer,stream.id,"Android microphone unavailable; check its permission or choose another input");}
  finally{if(record!=null){try{record.stop();}catch(Exception ignored){}record.release();}stream.running=false;streams.remove(stream.peer,stream);emit(stream.peer,message("mic-stop",stream.id));}
 }
 void stop(Stream stream){stream.running=false;streams.remove(stream.peer,stream);emit(stream.peer,message("mic-stop",stream.id));}
 synchronized void stopStream(){for(Stream stream:streams.values())stop(stream);}
 @Override public void onDestroy(){stopStream();instance=null;super.onDestroy();}
 @Override public IBinder onBind(Intent intent){return null;}
}
