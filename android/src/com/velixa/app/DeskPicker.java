package com.velixa.app;
import android.app.*;
import android.content.*;
import android.view.*;
import android.widget.*;
import org.json.*;
import java.util.*;

final class DeskPicker {
 static void show(MainActivity owner){
  if(InputService.instance==null){owner.consent();return;}
  try{SavedDesks.remember(owner);}catch(Exception ignored){}
  ArrayList<JSONObject> choices=new ArrayList<>();ArrayList<String> names=new ArrayList<>();JSONArray saved=SavedDesks.all(owner);
  for(int i=0;i<saved.length();i++){JSONObject row=saved.optJSONObject(i);if(row!=null){choices.add(row);names.add(row.optString("name",row.optString("host"))+" · Saved");}}
  ArrayAdapter<String> adapter=new ArrayAdapter<String>(owner,android.R.layout.simple_list_item_1,names){@Override public View getView(int position,View recycled,ViewGroup parent){TextView row=(TextView)super.getView(position,recycled,parent);row.setGravity(Gravity.CENTER_VERTICAL|Gravity.START);row.setIncludeFontPadding(false);row.setMinHeight(owner.dp(56));row.setPadding(owner.dp(20),owner.dp(14),owner.dp(20),owner.dp(14));return row;}};
  AlertDialog dialog=new AlertDialog.Builder(owner).setTitle("Switch desk").setMessage("Choose a saved or nearby desk. To create your own, open Velixa on Windows → Switch desk → Create my own desk, then pair this phone.")
   .setAdapter(adapter,(d,index)->{JSONObject target=choices.get(index);if(SavedDesks.find(owner,target.optString("id"))!=null){try{owner.prepareDeskSwitch();SavedDesks.activate(owner,target.getString("id"));InputService.instance.connect();}catch(Exception e){Toast.makeText(owner,"Could not open saved desk. Try pairing again.",Toast.LENGTH_LONG).show();}}else owner.pairWithCode(target.optString("host"));})
   .setPositiveButton("Scan QR code",(d,w)->owner.startActivityForResult(new Intent(owner,ScannerActivity.class),42))
   .setNeutralButton("Enter code",(d,w)->owner.pairWithCode())
   .setNegativeButton("Cancel",null).create();dialog.show();
  new Thread(()->{List<String[]> nearby=Connection.discover();owner.runOnUiThread(()->{if(owner.isFinishing()||owner.isDestroyed()||!dialog.isShowing())return;for(String[] peer:nearby){boolean found=false;for(int i=0;i<choices.size();i++)if(choices.get(i).optString("id").equals(peer[2])){names.set(i,peer[1]+" · Nearby / saved");found=true;break;}if(!found)try{choices.add(new JSONObject().put("id",peer[2]).put("host",peer[0]));names.add(peer[1]+" · Nearby / pair");}catch(JSONException ignored){}}adapter.notifyDataSetChanged();if(choices.isEmpty())dialog.setTitle("No nearby desks · scan QR or enter code");});},"Velixa desk discovery").start();
 }
}
