using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
partial struct PlayAndStopSoundElevationSystem : ISystem
{
    private float elevationRotationSFXInitialPitch;
    private float elevationRotationSFXInitialVolume;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<SFX_Elevation> sfx_Elevation, Entity entity) in SystemAPI.Query<RefRO<SFX_Elevation>>().WithEntityAccess())
        {
            elevationRotationSFXInitialPitch = sfx_Elevation.ValueRO.elevationRotationSFXInitialPitch;
            elevationRotationSFXInitialVolume = sfx_Elevation.ValueRO.elevationRotationSFXInitialVolume;
            AudioSource audioSource = state.World.EntityManager.GetComponentObject<AudioSource>(entity);
            if (sfx_Elevation.ValueRO.isPlaying)
            {
                if (!audioSource.isPlaying) audioSource.Play();

                audioSource.pitch = elevationRotationSFXInitialPitch;
                audioSource.volume = elevationRotationSFXInitialVolume;
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
