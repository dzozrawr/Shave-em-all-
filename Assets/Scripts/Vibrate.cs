using UnityEngine;
using System.Collections;

public class Vibrate
{

    public AndroidJavaClass unityPlayer;
    public AndroidJavaObject currentActivity;
    public AndroidJavaObject sysService;


    public Vibrate()
    {
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        try
        {
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        } catch { 
        }

        
        
        sysService = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
    }

    //Functions from https://developer.android.com/reference/android/os/Vibrator.html
    public void vibrate()
    {
        sysService.Call("vibrate");
    }


    public void vibrate(long milliseconds)
    {
        sysService.Call("vibrate", milliseconds);
    }

    public void vibrate(long[] pattern, int repeat)
    {
        sysService.Call("vibrate", pattern, repeat);
    }


    public void cancel()
    {
        sysService.Call("cancel");
    }

    public bool hasVibrator()
    {
        return sysService.Call<bool>("hasVibrator");
    }
}
