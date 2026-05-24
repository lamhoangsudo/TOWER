using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Mathematics;
using UnityEngine;

public static class FmodSoundManager
{
    public static void PlayOneShotFireSound(GUID guidSoundEvent, float pitch, float volume, float3 worldPosition)
    {
        EventInstance instance = RuntimeManager.CreateInstance(guidSoundEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes((Vector3)worldPosition));
        instance.setVolume(volume);
        instance.setPitch(pitch);
        instance.start();
        instance.release();
    }
    public static EventInstance GetEventInstance(GUID guidSoundEvent)
    {
        return RuntimeManager.CreateInstance(guidSoundEvent);
    }
    public static EventInstance GetEventInstance(EventReference soundEvent)
    {
        return RuntimeManager.CreateInstance(soundEvent);
    }
    public static void SetPositionEventInstance(EventInstance instance, float3 worldPosition)
    {
        instance.set3DAttributes(RuntimeUtils.To3DAttributes((Vector3)worldPosition));
    }
    public static void PlayEffectVolumeAndPitchSoundLoop(EventInstance instance, float volume, float pitch)
    {
        PlayEffectVolumeSoundLoop(instance, volume);
        PlayEffectPitchSoundLoop(instance, pitch);
    }
    public static void PlayEffectVolumeSoundLoop(EventInstance instance, float volume)
    {
        instance.setVolume(volume);
    }
    public static void PlayEffectPitchSoundLoop(EventInstance instance, float pitch)
    {
        instance.setPitch(pitch);
    }
    public static void PlaySoundEffectLoop(EventInstance instance)
    {
        instance.getPlaybackState(out PLAYBACK_STATE state);
        if(state != PLAYBACK_STATE.PLAYING) instance.start();
    }
    public static void StopSoundEffectLoop(EventInstance instance, FMOD.Studio.STOP_MODE STOP_MODE)
    {
        instance.getPlaybackState(out PLAYBACK_STATE state);
        if (state != PLAYBACK_STATE.STOPPED) instance.stop(STOP_MODE);
    }
    public static void SetParameterSoundEffectLoop(EventInstance instance, string name, float parameterValue)
    {
        instance.setParameterByName(name, parameterValue, true);    
    }

    /// <summary>
    /// Release event instance — gọi khi entity/turret bị destroy để tránh memory leak FMOD.
    /// </summary>
    public static void ReleaseEventInstance(EventInstance instance)
    {
        instance.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING)
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        instance.release();
    }

    /// <summary>
    /// Check if an EventInstance is valid (has been created and not yet released).
    /// </summary>
    public static bool IsInstanceValid(EventInstance instance)
    {
        return instance.isValid();
    }
}
