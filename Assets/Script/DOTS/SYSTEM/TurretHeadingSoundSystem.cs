using Unity.Burst;
using Unity.Entities;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretPitchSystem))]
partial struct TurretHeadingSoundSystem : ISystem
{
    private float HeadingRotationSFXInitialPitch;
    private float ElevationRotationSFXInitialPitch;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRW<Turret> turret in SystemAPI.Query<RefRW<Turret>>())
        {
            AudioSource audioSourceHeadingSFX = state.World.EntityManager.GetComponentObject<AudioSource>(turret.ValueRO.SFX_HeadingEntity);
            AudioSource audioSourceElevationSFX = state.World.EntityManager.GetComponentObject<AudioSource>(turret.ValueRO.SFX_ElevationEntity);
            if( audioSourceHeadingSFX == null || audioSourceElevationSFX == null)
            {
                Debug.LogWarning($"AudioSource components are missing on TurretSoundAuthoring for entity. Skipping sound updates.");
                continue;
            }
            Unity.Mathematics.Random random = turret.ValueRO.random;

            if (HeadingRotationSFXInitialPitch == 0) HeadingRotationSFXInitialPitch = audioSourceHeadingSFX.pitch * random.NextFloat(0.95f, 1.05f);
            if (ElevationRotationSFXInitialPitch == 0) ElevationRotationSFXInitialPitch = audioSourceElevationSFX.pitch * random.NextFloat(0.95f, 1.05f);

            turret.ValueRW.random = random;

            if (turret.ValueRO.IsHeadingRotationSFX)
            {
                if (!audioSourceHeadingSFX.isPlaying) audioSourceHeadingSFX.Play();
                audioSourceHeadingSFX.pitch = Mathf.Lerp(HeadingRotationSFXInitialPitch * 0.8f, HeadingRotationSFXInitialPitch, turret.ValueRO.headingSpeedFactor);
                audioSourceHeadingSFX.volume = Mathf.Lerp(0f, 1f, turret.ValueRO.headingSpeedFactor);
            }
            else
            {
                if (audioSourceHeadingSFX.isPlaying)
                {
                    audioSourceHeadingSFX.Stop();
                }
            }


            if (ElevationRotationSFXInitialPitch == 0) ElevationRotationSFXInitialPitch = audioSourceElevationSFX.pitch * random.NextFloat(0.95f, 1.05f);
            turret.ValueRW.random = random;
            if (turret.ValueRO.IsElevationRotationSFX)
            {
                if (!audioSourceElevationSFX.isPlaying) audioSourceElevationSFX.Play();
                audioSourceElevationSFX.pitch = Mathf.Lerp(ElevationRotationSFXInitialPitch * 0.8f, ElevationRotationSFXInitialPitch, turret.ValueRO.elevationSpeedFactor);
                audioSourceElevationSFX.volume = Mathf.Lerp(0f, 1f, turret.ValueRO.elevationSpeedFactor);
            }
            else
            {
                if (audioSourceElevationSFX.isPlaying)
                {
                    audioSourceElevationSFX.Stop();
                }
            }

        }
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
