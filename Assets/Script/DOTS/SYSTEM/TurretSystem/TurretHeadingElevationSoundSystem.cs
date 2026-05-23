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
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<SFX_Heading, SFX_Elevation>().Build());
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        TurretHeadingSoundJob turretHeadingSoundJob = new()
        {
            rotationLookup = SystemAPI.GetComponentLookup<TurretRotation>(isReadOnly: true),
        };
        turretHeadingSoundJob.ScheduleParallel();
        TurretElevationSoundJob turretElevationSoundJob = new()
        {
            rotationLookup = SystemAPI.GetComponentLookup<TurretRotation>(isReadOnly: true),
        };
        turretElevationSoundJob.ScheduleParallel();
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
[BurstCompile]
public partial struct TurretHeadingSoundJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<TurretRotation> rotationLookup;
    public void Execute(ref SFX_Heading SFX_Heading)
    {
        TurretRotation rotation = rotationLookup[SFX_Heading.turretEntity];
        if (rotation.IsHeadingRotationSFX)
        {
            if (!SFX_Heading.isPlaying) SFX_Heading.isPlaying = true;
            SFX_Heading.headingSpeedFactor = rotation.headingSpeedFactor;
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
    [ReadOnly] public ComponentLookup<TurretRotation> rotationLookup;
    public void Execute(ref SFX_Elevation SFX_Elevation)
    {
        TurretRotation rotation = rotationLookup[SFX_Elevation.turretEntity];
        if (rotation.IsElevationRotationSFX)
        {
            if (!SFX_Elevation.isPlaying) SFX_Elevation.isPlaying = true;
            SFX_Elevation.elevationSpeedFactor = rotation.elevationSpeedFactor;
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
