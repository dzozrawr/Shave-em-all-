using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static AudioClip shaverOnSound, shaverActionSound;
    public static AudioSource audioSrc;
    // Start is called before the first frame update
    void Start()
    {
        shaverOnSound = Resources.Load<AudioClip>("shaverOn");
        shaverActionSound = Resources.Load<AudioClip>("shaverAction");

        audioSrc = GetComponent<AudioSource>();
    }

    public static void PlaySound(string clip)
    {
        switch (clip)
        {
            case "shaverOn":
                if (audioSrc.isPlaying) return;
                audioSrc.PlayOneShot(shaverOnSound);
                break;
            case "shaverAction":
                if (audioSrc.isPlaying) return;
                audioSrc.PlayOneShot(shaverActionSound);
                break;
        }
    }

    public static void PlayLoopingSound(string clip)
    {
        switch (clip)
        {
            case "shaverOn":
                if (audioSrc.isPlaying) return;
                audioSrc.loop = true;
                audioSrc.clip = shaverOnSound;
                audioSrc.Play();
                break;
            case "shaverAction":
                if (audioSrc.isPlaying) return;
                audioSrc.loop = true;
                audioSrc.clip = shaverActionSound;
                audioSrc.Play();
                break;
        }
    }
    public static void StopLoopingSound()
    {
        if (!audioSrc.isPlaying || !audioSrc.loop) return;
        audioSrc.loop = false;
        audioSrc.Stop();
    }

    public static void StopLoopingSound(string clip)
    {
        switch (clip)
        {
            case "shaverOn":
                if (!audioSrc.isPlaying) return;
                if (audioSrc.clip != shaverOnSound) return;
                audioSrc.loop = false;
                // audioSrc.clip = shaverOnSound;
                audioSrc.Stop();
                break;
            case "shaverAction":
                if (!audioSrc.isPlaying) return;
                if (audioSrc.clip != shaverActionSound) return;
                audioSrc.loop = false;
                audioSrc.Stop();
                break;
                
        }
    }

    /*    public static void StopSound()
        {
            if (!audioSrc.isPlaying || audioSrc.loop) return;
            audioSrc.Stop();
        }*/




}
