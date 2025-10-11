using System;
using System.Collections;
using UnityEngine;
using FMOD;
using FMODUnity;

public class FMODRadioStreamer : MonoBehaviour
{
    private string streamUrl = "";
    private bool isMuted = false;
    private FMOD.Sound sound;
    private FMOD.Channel channel;
    private FMOD.ChannelGroup masterChannelGroup;
    private FMOD.System system;
    private bool isPlaying = false;
    private bool isInitialized = false;
    private bool hasSound = false;
    private bool hasChannel = false;
    
    public System.Action<bool> OnMuteStateChanged;
    
    void Update()
    {
        // Update FMOD system
        if (isInitialized)
        {
            try
            {
                system.update();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("FMOD update error: " + e.Message);
            }
        }
    }

    void OnDestroy()
    {
        StopStream();
    }
    
    void OnApplicationQuit()
    {
        // App is being quit - ensure complete cleanup
        StopStream();
#if !UNITY_EDITOR
        UnityEngine.Debug.Log("FMOD CoreSystem.release()");
        RuntimeManager.CoreSystem.release();
#endif
    }

    void InitializeFMOD()
    {
        try
        {
            // Get FMOD system from RuntimeManager
            system = RuntimeManager.CoreSystem;
            system.getMasterChannelGroup(out masterChannelGroup);
            isInitialized = true;
            UnityEngine.Debug.Log("FMOD initialized successfully");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Failed to initialize FMOD: " + e.Message);
            isInitialized = false;
        }
    }


    public void PlayStream(string url)
    {
        if (isPlaying)
        {
            StopStream();
        }
        streamUrl = url;
        PlayStream();
    }

    void PlayStream()
    {

        if (!isInitialized)
        {
            InitializeFMOD();
            if (!isInitialized)
            {
                UnityEngine.Debug.LogError("Failed to initialize FMOD");
                return;
            }
        }
        
        if (isPlaying)
        {
            StopStream();
        }
        
        if (string.IsNullOrEmpty(streamUrl))
        {
            UnityEngine.Debug.LogError("No stream URL provided");
            return;
        }
        
        StartCoroutine(LoadAndPlayStream());
    }

    IEnumerator LoadAndPlayStream()
    {
        UnityEngine.Debug.Log("Loading stream: " + streamUrl);
        
        // Create sound from URL
        FMOD.RESULT result = system.createSound(streamUrl, 
            MODE.CREATESTREAM, 
            out sound);
        
        UnityEngine.Debug.Log("CreateSound result: " + result);
        
        if (result != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError("Failed to create sound: " + result);
            hasSound = false;
            // Clean up any partial sound creation
            if (sound.handle != System.IntPtr.Zero)
            {
                sound.release();
            }
            yield break;
        }
        
        hasSound = true;
        
        // Wait for sound to load
        float timeout = 15f;
        float elapsed = 0f;
        
        while (sound.getOpenState(out OPENSTATE openState, out _, out _, out _) == FMOD.RESULT.OK)
        {
            if (elapsed >= timeout)
            {
                UnityEngine.Debug.LogError("Loading timeout after " + timeout + " seconds");
                if (hasSound)
                {
                    sound.release();
                    hasSound = false;
                }
                yield break;
            }
            
            if (openState == OPENSTATE.READY)
            {
                UnityEngine.Debug.Log("Stream loaded successfully!");
                break;
            }
            
            if (openState == OPENSTATE.ERROR)
            {
                UnityEngine.Debug.LogWarning("Stream loading error at " + elapsed + " seconds");
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Play the sound
        result = system.playSound(sound, masterChannelGroup, false, out channel);
        
        if (result == FMOD.RESULT.OK)
        {
            channel.setPriority(0);
            hasChannel = true;
            isPlaying = true;
            isMuted = false;
            UnityEngine.Debug.Log("Stream started successfully");
            OnMuteStateChanged?.Invoke(isMuted);
            
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to play: " + result);
            hasChannel = false;
        }
    }

    public void ToggleMuteStream()
    {
        if (isMuted)
        {
            WriteChannelState("before setMute(false)");
            FMOD.RESULT muteResult = channel.setMute(false);
            UnityEngine.Debug.Log("setMute(false) result: " + muteResult);
            WriteChannelState("after setMute(false)");
            //PlayStream();
            isMuted = false;
            UnityEngine.Debug.Log("Stream unmuted");
        }
        else
        {
            WriteChannelState("before setMute(true)");
            FMOD.RESULT muteResult = channel.setMute(true);
            UnityEngine.Debug.Log("setMute(true) result: " + muteResult);
            WriteChannelState("after setMute(true)");
            //StopStream();
            isMuted = true;
            UnityEngine.Debug.Log("Stream muted");
        }
        OnMuteStateChanged?.Invoke(isMuted);
    }

    public void StopStream()
    {
        if (hasChannel)
        {
            channel.stop();
            hasChannel = false;
            UnityEngine.Debug.Log("Channel stopped");
        }

        if (hasSound)
        {
            sound.release();
            hasSound = false;
            UnityEngine.Debug.Log("Sound released");
        }

        isPlaying = false;
        isMuted = false;
    }
    
    
    public bool IsMuted()
    {
        return isMuted;
    }
    
    private void WriteChannelState(string context)
    {
        channel.isPlaying(out bool playing);
        channel.getPaused(out bool paused);
        channel.isVirtual(out bool isVirtual);
        UnityEngine.Debug.Log("Channel " + context + ": isPlaying=" + playing + ", getPaused=" + paused + ", isVirtual=" + isVirtual);
    }
    
}
