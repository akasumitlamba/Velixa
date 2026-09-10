package com.velixa.app;
import android.content.Context;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;
public final class SecureStore {
 static SecretKey key() throws Exception {KeyStore ks=KeyStore.getInstance("AndroidKeyStore");ks.load(null);if(!ks.containsAlias("velixa-pairing")){KeyGenerator gen=KeyGenerator.getInstance("AES","AndroidKeyStore");gen.init(new KeyGenParameterSpec.Builder("velixa-pairing",KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build());gen.generateKey();}return (SecretKey)ks.getKey("velixa-pairing",null);}
 public static void put(Context c,String value) throws Exception {Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.ENCRYPT_MODE,key());String s=Base64.encodeToString(cipher.getIV(),Base64.NO_WRAP)+":"+Base64.encodeToString(cipher.doFinal(value.getBytes("UTF-8")),Base64.NO_WRAP);c.getSharedPreferences("velixa",0).edit().putString("secret",s).apply();}
 public static String get(Context c){try{String[] s=c.getSharedPreferences("velixa",0).getString("secret","").split(":");Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.DECRYPT_MODE,key(),new GCMParameterSpec(128,Base64.decode(s[0],0)));return new String(cipher.doFinal(Base64.decode(s[1],0)),"UTF-8");}catch(Exception e){return "";}}
}
