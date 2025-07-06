using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
            if (elevationRotationSFXInitialPitch == 0) elevationRotationSFXInitialPitch = audioSourceElevationSFX.ValueRO.elevationRotationSFXInitialPitch * randomElevationSFX.NextFloat(0.95f, 1.05f);
            audioSourceElevationSFX.ValueRW.random = randomElevationSFX;
            if (turret.ValueRO.IsElevationRotationSFX)
            {
                if (!audioSourceElevationSFX.ValueRO.isPlaying) audioSourceElevationSFX.ValueRW.isPlaying = true;
                audioSourceElevationSFX.ValueRW.elevationRotationSFXInitialPitch = Mathf.Lerp(elevationRotationSFXInitialPitch * 0.8f, elevationRotationSFXInitialPitch, turret.ValueRO.elevationSpeedFactor);
                audioSourceElevationSFX.ValueRW.elevationRotationSFXInitialVolume = Mathf.Lerp(0f, 1f, turret.ValueRO.elevationSpeedFactor);
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
            HeadingRotationSFXInitialPitch = HeadingRotationSFXInitialPitch,
            turretLookup = SystemAPI.GetComponentLookup<Turret>(isReadOnly: true),
        };
        //JobHandle turretHeadingSoundJobHandler = 
        turretHeadingSoundJob.ScheduleParallel();
        TurretElevationSoundJob turretElevationSoundJob = new()
        {
            ElevationRotationSFXInitialPitch = ElevationRotationSFXInitialPitch,
            turretLookup = SystemAPI.GetComponentLookup<Turret>(isReadOnly: true),
        };
        //JobHandle turretElevationSoundJobHandler = 
        turretElevationSoundJob.ScheduleParallel();
        //state.Dependency = JobHandle.CombineDependencies(turretHeadingSoundJobHandler, turretElevationSoundJobHandler);
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
    public float HeadingRotationSFXInitialPitch;
    public void Execute(ref SFX_Heading SFX_Heading)
    {
        Turret turret = turretLookup[SFX_Heading.turretEntity];
        Unity.Mathematics.Random randomHeadingSFX = SFX_Heading.random;
        if (HeadingRotationSFXInitialPitch == 0) HeadingRotationSFXInitialPitch = SFX_Heading.headingRotationSFXInitialPitch * randomHeadingSFX.NextFloat(0.95f, 1.05f);
        SFX_Heading.random = randomHeadingSFX;

        if (turret.IsHeadingRotationSFX)
        {
            if (!SFX_Heading.isPlaying) SFX_Heading.isPlaying = true;
            SFX_Heading.headingRotationSFXInitialPitch = math.lerp(0f, 1f, turret.headingSpeedFactor);
            SFX_Heading.headingRotationSFXInitialVolume = math.lerp(0f, 1f, turret.headingSpeedFactor);
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
    public float ElevationRotationSFXInitialPitch;
    public void Execute(ref SFX_Elevation SFX_Elevation)
    {
        Turret turret = turretLookup[SFX_Elevation.turretEntity];
        Unity.Mathematics.Random randomHeadingSFX = SFX_Elevation.random;
        if (ElevationRotationSFXInitialPitch == 0) ElevationRotationSFXInitialPitch = SFX_Elevation.elevationRotationSFXInitialPitch * randomHeadingSFX.NextFloat(0.95f, 1.05f);
        SFX_Elevation.random = randomHeadingSFX;

        if (turret.IsHeadingRotationSFX)
        {
            if (!SFX_Elevation.isPlaying) SFX_Elevation.isPlaying = true;
            SFX_Elevation.elevationRotationSFXInitialPitch = math.lerp(0f, 1f, turret.elevationSpeedFactor);
            SFX_Elevation.elevationRotationSFXInitialVolume = math.lerp(0f, 1f, turret.elevationSpeedFactor);
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
