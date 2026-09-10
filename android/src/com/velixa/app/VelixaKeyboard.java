package com.velixa.app;
import android.inputmethodservice.InputMethodService;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.*;
public final class VelixaKeyboard extends InputMethodService {
 public static VelixaKeyboard current;
 @Override public void onCreate(){super.onCreate();current=this;}
 @Override public void onDestroy(){current=null;super.onDestroy();}
 @Override public View onCreateInputView(){LinearLayout box=new LinearLayout(this);box.setPadding(20,12,20,12);box.setOrientation(LinearLayout.VERTICAL);TextView text=new TextView(this);text.setText("Velixa · type with your Windows keyboard");text.setTextSize(16);box.addView(text);Button button=new Button(this);button.setText("Use my touchscreen keyboard");button.setOnClickListener(v->((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).showInputMethodPicker());box.addView(button);return box;}
 @Override public boolean onEvaluateFullscreenMode(){return false;}
}
