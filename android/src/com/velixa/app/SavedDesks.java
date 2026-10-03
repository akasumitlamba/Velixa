package com.velixa.app;
import android.content.Context;
import org.json.*;
public final class SavedDesks {
 public static synchronized JSONArray all(Context c){try{return new JSONArray(SecureStore.get(c,"saved-desks"));}catch(Exception e){return new JSONArray();}}
 public static synchronized void remember(Context c) throws Exception {
  String active=SecureStore.get(c);if(active.isEmpty())return;JSONObject desk=new JSONObject(active);if(desk.optString("id").isEmpty()||desk.optString("token").length()!=64||desk.optBoolean("initial"))return;
  desk.put("host",c.getSharedPreferences("velixa",0).getString("host",""));desk.put("name",c.getSharedPreferences("velixa",0).getString("desk-name",desk.optString("host")));
  JSONArray previous=all(c),next=new JSONArray();for(int i=0;i<previous.length();i++){JSONObject row=previous.getJSONObject(i);if(!row.optString("id").equals(desk.optString("id")))next.put(row);}next.put(desk);SecureStore.put(c,"saved-desks",next.toString());
 }
 public static synchronized JSONObject find(Context c,String id){JSONArray desks=all(c);for(int i=0;i<desks.length();i++){JSONObject desk=desks.optJSONObject(i);if(desk!=null&&desk.optString("id").equals(id))return desk;}return null;}
 public static synchronized void activate(Context c,String id) throws Exception {JSONObject desk=find(c,id);if(desk==null)throw new IllegalArgumentException("Desk is not paired");SecureStore.put(c,desk.toString());c.getSharedPreferences("velixa",0).edit().putString("host",desk.getString("host")).putString("desk-name",desk.optString("name",desk.getString("host"))).apply();}
}
