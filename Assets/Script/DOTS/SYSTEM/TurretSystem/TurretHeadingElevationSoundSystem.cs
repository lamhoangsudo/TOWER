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
