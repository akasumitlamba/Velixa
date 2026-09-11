package com.velixa.app;
import java.util.*;
import static android.media.AudioDeviceInfo.*;
public final class MicrophoneRoutesTest {
 static int checks;static void check(boolean ok,String name){if(!ok)throw new AssertionError(name);checks++;System.out.println("PASS "+name);}
 static MicrophoneRoutes.Route r(int id,int type,String address,String name){return new MicrophoneRoutes.Route(id,type,address,name,true);}
 public static void main(String[] args){
  List<MicrophoneRoutes.Route> phone=Arrays.asList(r(8,TYPE_BUILTIN_MIC,"back","moto e(7) plus"),r(3,TYPE_BUILTIN_MIC,"bottom","moto e(7) plus"),r(4,TYPE_TELEPHONY,"","moto e(7) plus"),r(5,TYPE_FM_TUNER,"","moto e(7) plus"),r(6,TYPE_REMOTE_SUBMIX,"","moto e(7) plus"));
  List<MicrophoneRoutes.Route> selected=MicrophoneRoutes.select(phone);check(selected.size()==1,"phone built-in routes become one microphone; telephony/FM/mix excluded");check(selected.get(0).id==3,"deterministic actual source ID preserved");
  Collections.reverse(phone);check(MicrophoneRoutes.select(phone).get(0).id==3,"enumeration order cannot change selection");
  check(MicrophoneRoutes.label(TYPE_BUILTIN_MIC,"moto e(7) plus","moto e(7) plus").equals("System microphone"),"phone name is not repeated in microphone label");
  selected=MicrophoneRoutes.select(Arrays.asList(r(1,TYPE_USB_DEVICE,"card=1;device=0","USB Mic"),r(2,TYPE_USB_HEADSET,"card=1;device=0","USB Mic")));check(selected.size()==1,"duplicate USB routes share one physical address");
  selected=MicrophoneRoutes.select(Arrays.asList(r(1,TYPE_USB_DEVICE,"card=1","Same Mic"),r(2,TYPE_USB_DEVICE,"card=2","Same Mic")));check(selected.size()==2,"different physical USB microphones remain selectable");
  selected=MicrophoneRoutes.select(Arrays.asList(r(1,TYPE_USB_DEVICE,"","Same Mic"),r(2,TYPE_USB_DEVICE,"","Same Mic")));check(selected.size()==2,"unknown addresses do not merge distinct hardware by name");
  check(MicrophoneRoutes.select(Arrays.asList(r(1,TYPE_WIRED_HEADSET,"","Headset"),r(2,TYPE_BLUETOOTH_SCO,"aa:bb","Wireless"),r(3,TYPE_BLE_HEADSET,"cc:dd","LE headset"))).size()==3,"wired and Bluetooth microphone inputs remain available");
  check(MicrophoneRoutes.select(Arrays.asList(new MicrophoneRoutes.Route(1,TYPE_USB_HEADSET,"1","Output",false))).isEmpty(),"output-only device excluded");
  check(MicrophoneRoutes.select(Arrays.asList(r(1,TYPE_BUS,"","bus"),r(2,TYPE_IP,"","ip"),r(3,TYPE_UNKNOWN,"","unknown"))).isEmpty(),"virtual and unknown inputs are not mislabeled as microphones");
  check(MicrophoneRoutes.select(Collections.<MicrophoneRoutes.Route>emptyList()).isEmpty(),"no hardware produces no invented choices");
  check(MicrophoneRoutes.label(TYPE_USB_HEADSET,"Studio USB","Motorola").equals("USB microphone · Studio USB"),"external microphone retains its own name");
  System.out.println(checks+" microphone route checks passed");
 }
}
