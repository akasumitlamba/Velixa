package com.velixa.app;

import android.app.*;
import android.content.*;
import android.os.*;

/** Keeps an explicitly connected desk alive while the activity is in the background. */
public final class ConnectionService extends Service {
 private final Handler handler = new Handler(Looper.getMainLooper());
 private PowerManager.WakeLock wake;
 private final Runnable renew = new Runnable() {
  public void run() {
   if (InputService.instance == null || !getSharedPreferences("velixa", 0).getBoolean("connected", false)) {
    stopSelf();
    return;
   }
   // Bounded leases expire even if lifecycle cleanup is interrupted.
   wake.acquire(10 * 60 * 1000L);
   handler.postDelayed(this, 5 * 60 * 1000L);
  }
 };
 @Override public void onCreate() {
  super.onCreate();
  NotificationManager notifications = (NotificationManager)getSystemService(NOTIFICATION_SERVICE);
  notifications.createNotificationChannel(new NotificationChannel("desk-connection", "Desk connection", NotificationManager.IMPORTANCE_LOW));
  wake = ((PowerManager)getSystemService(POWER_SERVICE)).newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, "Velixa:desk-connection");
  wake.setReferenceCounted(false);
 }
 @Override public int onStartCommand(Intent intent, int flags, int startId) {
  if (intent != null && "disconnect".equals(intent.getAction())) {
   if (InputService.instance != null) InputService.instance.disconnect(true);
   stopSelf();
   return START_NOT_STICKY;
  }
  PendingIntent open = PendingIntent.getActivity(this, 0, new Intent(this, MainActivity.class), PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);
  PendingIntent stop = PendingIntent.getService(this, 1, new Intent(this, ConnectionService.class).setAction("disconnect"), PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);
  Notification notification = new Notification.Builder(this, "desk-connection")
   .setSmallIcon(android.R.drawable.ic_menu_share).setContentTitle("Velixa desk connection")
   .setContentText("Ready to reconnect automatically to your paired PC.")
   .setContentIntent(open).setOngoing(true)
   .addAction(new Notification.Action.Builder(null, "Disconnect", stop).build()).build();
  startForeground(28, notification);
  handler.removeCallbacks(renew);
  renew.run();
  return START_STICKY;
 }
 @Override public void onDestroy() {
  handler.removeCallbacksAndMessages(null);
  if (wake != null && wake.isHeld()) wake.release();
  super.onDestroy();
 }
 @Override public IBinder onBind(Intent intent) { return null; }
}
