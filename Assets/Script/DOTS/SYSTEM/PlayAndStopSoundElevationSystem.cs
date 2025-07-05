using Unity.Burst;
using Unity.Entities;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
partial struct PlayAndStopSoundElevationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<SFX_Elevation> SFX_Elevation, Entity Entity) in SystemAPI.Query<RefRO<SFX_Elevation>>().WithEntityAccess())
        {
            AudioSource audioSource = state.EntityManager.GetComponentObject<AudioSource>(Entity);
            if (SFX_Elevation.ValueRO.isPlaying)
            {
                if (!audioSource.isPlaying)
                {
                    UnityEngine.Debug.Log($"Play sound: {audioSource.clip?.name}");
                    audioSource.Play();
                }
                audioSource.pitch = SFX_Elevation.ValueRO.ElevationRotationSFXInitialPitch;
                audioSource.volume = SFX_Elevation.ValueRO.ElevationRotationSFXInitialVolume;
            }
            else
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
