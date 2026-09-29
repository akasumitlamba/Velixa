using System;
using System.Runtime.InteropServices;
namespace Velixa {
// Process loopback excludes Velixa playback, so microphone injection cannot feed
// back into the outgoing speaker stream. Callback data is stereo 48 kHz PCM16.
public sealed class ProcessAudio:IDisposable {
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void Callback(IntPtr bytes,int count);
 [DllImport("Velixa.Audio.dll",CallingConvention=CallingConvention.StdCall)] static extern int AudioStart(uint excluded,Callback callback,out IntPtr capture);
 [DllImport("Velixa.Audio.dll",CallingConvention=CallingConvention.StdCall)] static extern int AudioStatus(IntPtr capture);
 [DllImport("Velixa.Audio.dll",CallingConvention=CallingConvention.StdCall)] static extern void AudioStop(IntPtr capture);
 Callback callback;IntPtr handle;Exception failure;
 public ProcessAudio(Action<byte[]> data){callback=(pointer,count)=>{try{var bytes=new byte[count];Marshal.Copy(pointer,bytes,0,count);data(bytes);}catch(Exception e){failure=e;}};int hr=AudioStart((uint)System.Diagnostics.Process.GetCurrentProcess().Id,callback,out handle);if(hr<0)throw new InvalidOperationException("Windows could not start system audio sharing. This feature requires Windows 11 and a running Windows Audio service.",Marshal.GetExceptionForHR(hr));}
 public void Check(){if(failure!=null)throw new InvalidOperationException("Audio transport stopped.",failure);if(handle!=IntPtr.Zero)Marshal.ThrowExceptionForHR(AudioStatus(handle));}
 public void Dispose(){if(handle==IntPtr.Zero)return;AudioStop(handle);handle=IntPtr.Zero;GC.KeepAlive(callback);}
}
}

