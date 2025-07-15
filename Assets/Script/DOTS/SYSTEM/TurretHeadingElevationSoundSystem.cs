using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretElevationSystem))]
partial struct TurretHeadingElevationSoundSystem : ISystem
{
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
            RefRW<SFX_Elevation> audioSourceHeadingSFX = SystemAPI.GetComponentRW<SFX_Elevation>(turret.ValueRO.SFX_HeadingEntity);
            RefRW<SFX_Elevation> audioSourceElevationSFX = SystemAPI.GetComponentRW<SFX_Elevation>(turret.ValueRO.SFX_ElevationEntity);
            #region heading
            Unity.Mathematics.Random randomHeadingSFX = audioSourceHeadingSFX.ValueRO.random;
            if (headingRotationSFXInitialPitch == 0) headingRotationSFXInitialPitch = audioSourceHeadingSFX.ValueRO.headingRotationSFXInitialPitch * randomHeadingSFX.NextFloat(0.95f, 1.05f);
            audioSourceHeadingSFX.ValueRW.random = randomHeadingSFX;

            if (turret.ValueRO.IsHeadingRotationSFX)
            {
                if (!audioSourceHeadingSFX.ValueRO.isPlaying) audioSourceHeadingSFX.ValueRW.isPlaying = true;
                audioSourceHeadingSFX.ValueRW.headingRotationSFXInitialPitch = Mathf.Lerp(headingRotationSFXInitialPitch * 0.8f, headingRotationSFXInitialPitch, turret.ValueRO.headingSpeedFactor);
                audioSourceHeadingSFX.ValueRW.headingRotationSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.ValueRO.headingSpeedFactor);
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
            if (GatlingSpinSFXInitialPitch == 0) GatlingSpinSFXInitialPitch = audioSourceElevationSFX.ValueRO.GatlingSpinSFXInitialPitch * randomElevationSFX.NextFloat(0.95f, 1.05f);
            audioSourceElevationSFX.ValueRW.random = randomElevationSFX;
            if (turret.ValueRO.IsElevationRotationSFX)
            {
                if (!audioSourceElevationSFX.ValueRO.isPlaying) audioSourceElevationSFX.ValueRW.isPlaying = true;
                audioSourceElevationSFX.ValueRW.GatlingSpinSFXInitialPitch = Mathf.Lerp(GatlingSpinSFXInitialPitch * 0.8f, GatlingSpinSFXInitialPitch, turret.ValueRO.elevationSpeedFactor);
                audioSourceElevationSFX.ValueRW.GatlingSpinSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.ValueRO.elevationSpeedFactor);
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
        TurretHeadingSoundJob turretHeadingSoundJob = new()
        {
            turretLookup = SystemAPI.GetComponentLookup<Turret>(isReadOnly: true),
        };
        turretHeadingSoundJob.ScheduleParallel();
        TurretElevationSoundJob turretElevationSoundJob = new()
        {
            turretLookup = SystemAPI.GetComponentLookup<Turret>(isReadOnly: true),
        };
        turretElevationSoundJob.ScheduleParallel();
        #endregion
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
[BurstCompile]
public partial struct TurretHeadingSoundJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Turret> turretLookup;
    public void Execute(ref SFX_Heading SFX_Heading)
    {
        Turret turret = turretLookup[SFX_Heading.turretEntity];
        if (turret.IsHeadingRotationSFX)
        {
            if (!SFX_Heading.isPlaying) SFX_Heading.isPlaying = true;
            SFX_Heading.headingSpeedFactor = turret.headingSpeedFactor;
        }
        else
        {
            if (SFX_Heading.isPlaying)
            {
                SFX_Heading.isPlaying = false;
            }
        }
    }
}
[BurstCompile]
public partial struct TurretElevationSoundJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Turret> turretLookup;
    public void Execute(ref SFX_Elevation SFX_Elevation)
    {
        Turret turret = turretLookup[SFX_Elevation.turretEntity];
        if (turret.IsElevationRotationSFX)
        {
            if (!SFX_Elevation.isPlaying) SFX_Elevation.isPlaying = true;
            SFX_Elevation.elevationSpeedFactor = turret.elevationSpeedFactor;
        }
        else
        {
            if (SFX_Elevation.isPlaying)
            {
                SFX_Elevation.isPlaying = false;
            }
        }
    }
}
