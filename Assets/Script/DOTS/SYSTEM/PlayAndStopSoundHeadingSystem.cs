using Unity.Burst;
using Unity.Entities;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
partial struct PlayAndStopSoundHeadingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<SFX_Heading> SFX_Heading, Entity Entity) in SystemAPI.Query<RefRO<SFX_Heading>>().WithEntityAccess())
        {
            AudioSource audioSource = state.EntityManager.GetComponentObject<AudioSource>(Entity);
            if (SFX_Heading.ValueRO.isPlaying)
            {
                if (!audioSource.isPlaying)
                {
                    UnityEngine.Debug.Log($"Play sound: { audioSource.clip?.name}");
                    audioSource.Play();
                }
                audioSource.pitch = SFX_Heading.ValueRO.HeadingRotationSFXInitialPitch;
                audioSource.volume = SFX_Heading.ValueRO.HeadingRotationSFXInitialVolume;
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
