package com.velixa.app;
import android.media.AudioDeviceInfo;
import java.util.*;

/** Microphone choices, rather than every input route exposed by an audio HAL. */
public final class MicrophoneRoutes {
 public static final class Route {
  public final int id,type;public final String address,name;public final boolean source;
  public Route(int id,int type,String address,String name,boolean source){this.id=id;this.type=type;this.address=address==null?"":address.trim();this.name=name==null?"":name.trim();this.source=source;}
 }
 public static String category(int type){switch(type){
  case AudioDeviceInfo.TYPE_BUILTIN_MIC:return "System microphone";
  case AudioDeviceInfo.TYPE_USB_DEVICE:case AudioDeviceInfo.TYPE_USB_HEADSET:return "USB microphone";
  case AudioDeviceInfo.TYPE_WIRED_HEADSET:return "Headset microphone";
  case AudioDeviceInfo.TYPE_BLUETOOTH_SCO:case AudioDeviceInfo.TYPE_BLE_HEADSET:return "Bluetooth microphone";
  default:return null;
 }}
 public static List<Route> select(List<Route> routes){
  List<Route> ordered=new ArrayList<>(routes);Collections.sort(ordered,(a,b)->Integer.compare(a.id,b.id));
  Map<String,Route> unique=new LinkedHashMap<>();
  for(Route r:ordered){String category=category(r.type);if(!r.source||category==null)continue;
   String key=r.type==AudioDeviceInfo.TYPE_BUILTIN_MIC?"built-in":category+":"+(r.address.isEmpty()?"id:"+r.id:r.address.toLowerCase(Locale.ROOT));
   if(!unique.containsKey(key))unique.put(key,r);
  }
  return new ArrayList<>(unique.values());
 }
 public static String label(int type,String name,String deviceName){String category=category(type);if(category==null)return "";if(type==AudioDeviceInfo.TYPE_BUILTIN_MIC||name==null||name.trim().isEmpty()||name.trim().equalsIgnoreCase(deviceName))return category;return category+" · "+name.trim();}
}
