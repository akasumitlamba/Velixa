package com.velixa.app;
import android.app.*;
import android.os.*;
import android.content.*;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.hardware.Camera;
import android.view.*;
import android.widget.*;
import com.google.zxing.*;
import com.google.zxing.common.HybridBinarizer;
import java.util.*;
public final class ScannerActivity extends Activity implements SurfaceHolder.Callback,Camera.PreviewCallback {
 Camera camera;SurfaceView preview;boolean decoding,done;TextView hint;
 @Override public void onCreate(Bundle state){super.onCreate(state);getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN,WindowManager.LayoutParams.FLAG_FULLSCREEN);FrameLayout root=new FrameLayout(this);root.setBackgroundColor(Color.BLACK);preview=new SurfaceView(this);root.addView(preview,new FrameLayout.LayoutParams(-1,-1));hint=new TextView(this);hint.setText("Scan the QR code in Velixa on Windows\nAdd device → Android");hint.setTextSize(18);hint.setTextColor(Color.WHITE);hint.setGravity(Gravity.CENTER);hint.setPadding(24,36,24,36);hint.setBackgroundColor(0xCC141218);FrameLayout.LayoutParams p=new FrameLayout.LayoutParams(-1,-2,Gravity.TOP);root.addView(hint,p);Button cancel=new Button(this);cancel.setText("Cancel");cancel.setOnClickListener(v->finish());FrameLayout.LayoutParams b=new FrameLayout.LayoutParams(-1,-2,Gravity.BOTTOM);root.addView(cancel,b);setContentView(root);preview.getHolder().addCallback(this);if(checkSelfPermission(android.Manifest.permission.CAMERA)!=PackageManager.PERMISSION_GRANTED)requestPermissions(new String[]{android.Manifest.permission.CAMERA},1);}
 @Override public void onRequestPermissionsResult(int request,String[] permissions,int[] grants){super.onRequestPermissionsResult(request,permissions,grants);if(grants.length>0&&grants[0]==PackageManager.PERMISSION_GRANTED)open();else{hint.setText("Camera access is needed to scan your PC’s QR code.");}}
 void open(){if(camera!=null||done||checkSelfPermission(android.Manifest.permission.CAMERA)!=PackageManager.PERMISSION_GRANTED||!preview.getHolder().getSurface().isValid())return;try{int index=0;Camera.CameraInfo info=new Camera.CameraInfo();for(int i=0;i<Camera.getNumberOfCameras();i++){Camera.getCameraInfo(i,info);if(info.facing==Camera.CameraInfo.CAMERA_FACING_BACK){index=i;break;}}Camera.getCameraInfo(index,info);camera=Camera.open(index);Camera.Parameters params=camera.getParameters();Camera.Size chosen=params.getPreviewSize();for(Camera.Size size:params.getSupportedPreviewSizes())if(size.width<=1280&&size.width>=640&&(chosen.width>1280||size.width>chosen.width))chosen=size;params.setPreviewSize(chosen.width,chosen.height);if(params.getSupportedFocusModes().contains(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE))params.setFocusMode(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE);camera.setParameters(params);int rotation=getWindowManager().getDefaultDisplay().getRotation()*90;camera.setDisplayOrientation((info.orientation-rotation+360)%360);camera.setPreviewDisplay(preview.getHolder());camera.setPreviewCallback(this);camera.startPreview();}catch(Exception e){close();hint.setText("Could not open the camera. Close other camera apps and try again.");}}
 void close(){if(camera!=null){camera.setPreviewCallback(null);camera.stopPreview();camera.release();camera=null;}}
 @Override public void onPreviewFrame(byte[] data,Camera source){if(decoding||done)return;decoding=true;Camera.Size size=source.getParameters().getPreviewSize();byte[] copy=data.clone();new Thread(()->{String result=null;try{PlanarYUVLuminanceSource image=new PlanarYUVLuminanceSource(copy,size.width,size.height,0,0,size.width,size.height,false);MultiFormatReader reader=new MultiFormatReader();Map<DecodeHintType,Object> hints=new EnumMap<>(DecodeHintType.class);hints.put(DecodeHintType.POSSIBLE_FORMATS,Collections.singletonList(BarcodeFormat.QR_CODE));hints.put(DecodeHintType.TRY_HARDER,true);result=reader.decode(new BinaryBitmap(new HybridBinarizer(image)),hints).getText();}catch(Exception ignored){}final String value=result;runOnUiThread(()->{decoding=false;if(isFinishing()||done)return;if(value!=null){if(value.startsWith("velixa://pair/")&&value.length()<4096){done=true;setResult(RESULT_OK,new Intent().putExtra("qr",value));finish();}else hint.setText("That isn’t a Velixa pairing code.\nOpen Add device on your PC.");}});}).start();}
 @Override public void surfaceCreated(SurfaceHolder h){open();}
 @Override public void surfaceChanged(SurfaceHolder h,int format,int width,int height){}
 @Override public void surfaceDestroyed(SurfaceHolder h){close();}
 @Override protected void onPause(){close();super.onPause();}
 @Override protected void onResume(){super.onResume();open();}
}
