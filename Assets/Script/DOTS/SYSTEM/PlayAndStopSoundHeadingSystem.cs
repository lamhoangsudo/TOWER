using Unity.Burst;
using Unity.Entities;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
partial struct PlayAndStopSoundHeadingSystem : ISystem
{
    private float headingRotationSFXInitialPitch;
    private float headingRotationSFXInitialVolume;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<SFX_Heading> sfx_Heading, Entity entity) in SystemAPI.Query<RefRO<SFX_Heading>>().WithEntityAccess())
        {
            headingRotationSFXInitialPitch = sfx_Heading.ValueRO.headingRotationSFXInitialPitch;
            headingRotationSFXInitialVolume = sfx_Heading.ValueRO.headingRotationSFXInitialVolume;
            AudioSource audioSource = state.World.EntityManager.GetComponentObject<AudioSource>(entity);
            if (sfx_Heading.ValueRO.isPlaying)
            {
                if (!audioSource.isPlaying) audioSource.Play();

                audioSource.pitch = headingRotationSFXInitialPitch;
                audioSource.volume = headingRotationSFXInitialVolume;
            }
            else
            {
                if (audioSource.isPlaying) audioSource.Stop();
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
