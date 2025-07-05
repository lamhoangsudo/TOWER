using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretPitchSystem))]
partial struct TurretHeadingElevationSoundSystem : ISystem
{
    private float HeadingRotationSFXInitialPitch;
    private float ElevationRotationSFXInitialPitch;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        #region old code
        /*
        foreach (RefRO<Turret> turret in SystemAPI.Query<RefRO<Turret>>())
        {
            RefRW<SFX_Heading> audioSourceHeadingSFX = SystemAPI.GetComponentRW<SFX_Heading>(turret.ValueRO.SFX_HeadingEntity);
            RefRW<SFX_Elevation> audioSourceElevationSFX = SystemAPI.GetComponentRW<SFX_Elevation>(turret.ValueRO.SFX_ElevationEntity);
            #region heading
            Unity.Mathematics.Random randomHeadingSFX = audioSourceHeadingSFX.ValueRO.random;
            if (HeadingRotationSFXInitialPitch == 0) HeadingRotationSFXInitialPitch = audioSourceHeadingSFX.ValueRO.HeadingRotationSFXInitialPitch * randomHeadingSFX.NextFloat(0.95f, 1.05f);
            audioSourceHeadingSFX.ValueRW.random = randomHeadingSFX;

            if (turret.ValueRO.IsHeadingRotationSFX)
            {
                if (!audioSourceHeadingSFX.ValueRO.isPlaying) audioSourceHeadingSFX.ValueRW.isPlaying = true;
                audioSourceHeadingSFX.ValueRW.HeadingRotationSFXInitialPitch = Mathf.Lerp(HeadingRotationSFXInitialPitch * 0.8f, HeadingRotationSFXInitialPitch, turret.ValueRO.headingSpeedFactor);
                audioSourceHeadingSFX.ValueRW.HeadingRotationSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.ValueRO.headingSpeedFactor);
            }
            else
            {
                if (audioSourceHeadingSFX.ValueRO.isPlaying)
                {
                    audioSourceHeadingSFX.ValueRW.isPlaying = false;
                }
            }
            #endregion
            #region elevation
            Unity.Mathematics.Random randomElevationSFX = audioSourceElevationSFX.ValueRO.random;
            if (ElevationRotationSFXInitialPitch == 0) ElevationRotationSFXInitialPitch = audioSourceElevationSFX.ValueRO.ElevationRotationSFXInitialPitch * randomElevationSFX.NextFloat(0.95f, 1.05f);
            audioSourceElevationSFX.ValueRW.random = randomElevationSFX;
            if (turret.ValueRO.IsElevationRotationSFX)
            {
                if (!audioSourceElevationSFX.ValueRO.isPlaying) audioSourceElevationSFX.ValueRW.isPlaying = true;
                audioSourceElevationSFX.ValueRW.ElevationRotationSFXInitialPitch = Mathf.Lerp(ElevationRotationSFXInitialPitch * 0.8f, ElevationRotationSFXInitialPitch, turret.ValueRO.elevationSpeedFactor);
                audioSourceElevationSFX.ValueRW.ElevationRotationSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.ValueRO.elevationSpeedFactor);
            }
            else
            {
                if (audioSourceElevationSFX.ValueRO.isPlaying)
                {
                    audioSourceElevationSFX.ValueRW.isPlaying = false;
                }
            }
            #endregion
        }
        */
        #endregion
        #region new code
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretHeadingElevationSoundJob turretHeadingElevationSoundJob = new()
        {
            audioSourceHeadingSFXLookup = SystemAPI.GetComponentLookup<SFX_Heading>(isReadOnly: true),
            audioSourceElevationSFXLookup = SystemAPI.GetComponentLookup<SFX_Elevation>(isReadOnly: true),
            HeadingRotationSFXInitialPitch = HeadingRotationSFXInitialPitch,
            ElevationRotationSFXInitialPitch = ElevationRotationSFXInitialPitch,
            ecb = ecb.AsParallelWriter()
        };
        turretHeadingElevationSoundJob.ScheduleParallel();
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        #endregion
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
[BurstCompile]
public partial struct TurretHeadingElevationSoundJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<SFX_Heading> audioSourceHeadingSFXLookup;
    [ReadOnly] public ComponentLookup<SFX_Elevation> audioSourceElevationSFXLookup;
    public float HeadingRotationSFXInitialPitch;
    public float ElevationRotationSFXInitialPitch;
    public EntityCommandBuffer.ParallelWriter ecb;
    public void Execute(in Turret turret, [ChunkIndexInQuery]int sortkey)
    {
        SFX_Heading audioSourceHeadingSFXWriter = audioSourceHeadingSFXLookup[turret.SFX_HeadingEntity];
        SFX_Elevation audioSourceElevationSFXWriter = audioSourceElevationSFXLookup[turret.SFX_ElevationEntity];
        #region heading
        Unity.Mathematics.Random randomHeadingSFX = audioSourceHeadingSFXWriter.random;
        if (HeadingRotationSFXInitialPitch == 0) HeadingRotationSFXInitialPitch = audioSourceHeadingSFXWriter.HeadingRotationSFXInitialPitch * randomHeadingSFX.NextFloat(0.95f, 1.05f);
        audioSourceHeadingSFXWriter.random = randomHeadingSFX;

        if (turret.IsHeadingRotationSFX)
        {
            if (!audioSourceHeadingSFXWriter.isPlaying) audioSourceHeadingSFXWriter.isPlaying = true;
            audioSourceHeadingSFXWriter.HeadingRotationSFXInitialPitch = Mathf.Lerp(HeadingRotationSFXInitialPitch * 0.8f, HeadingRotationSFXInitialPitch, turret.headingSpeedFactor);
            audioSourceHeadingSFXWriter.HeadingRotationSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.headingSpeedFactor);
        }
        else
        {
            if (audioSourceHeadingSFXWriter.isPlaying)
            {
                audioSourceHeadingSFXWriter.isPlaying = false;
            }
        }
        #endregion
        #region elevation
        Unity.Mathematics.Random randomElevationSFX = audioSourceElevationSFXWriter.random;
        if (ElevationRotationSFXInitialPitch == 0) ElevationRotationSFXInitialPitch = audioSourceElevationSFXWriter.ElevationRotationSFXInitialPitch * randomElevationSFX.NextFloat(0.95f, 1.05f);
        audioSourceElevationSFXWriter.random = randomElevationSFX;
        if (turret.IsElevationRotationSFX)
        {
            if (!audioSourceElevationSFXWriter.isPlaying) audioSourceElevationSFXWriter.isPlaying = true;
            audioSourceElevationSFXWriter.ElevationRotationSFXInitialPitch = Mathf.Lerp(ElevationRotationSFXInitialPitch * 0.8f, ElevationRotationSFXInitialPitch, turret.elevationSpeedFactor);
            audioSourceElevationSFXWriter.ElevationRotationSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.elevationSpeedFactor);
        }
        else
        {
            if (audioSourceElevationSFXWriter.isPlaying)
            {
                audioSourceElevationSFXWriter.isPlaying = false;
            }
        }
        #endregion
        ecb.SetComponent(sortkey, turret.SFX_HeadingEntity, audioSourceHeadingSFXWriter);
        ecb.SetComponent(sortkey, turret.SFX_ElevationEntity, audioSourceElevationSFXWriter);
    }
}
