using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Xoay elevation pivot theo targeting.elevationAim.
/// Chỉ làm rotation — KHÔNG tính targeting.
/// </summary>
[UpdateAfter(typeof(TurretTargetingSystem))]
partial struct TurretElevationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<TurretRotation, TurretTargeting>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretElevationJob turretElevationJob = new()
        {
            deltaTime = deltaTime,
            pivotTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            ecb = ecb.AsParallelWriter()
        };
        JobHandle jobHandle = turretElevationJob.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct TurretElevationJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public ComponentLookup<LocalTransform> pivotTransformLookup;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        ref TurretRotation rotation,
        ref TurretTargeting targeting,
        [ChunkIndexInQuery] int sortkey
    )
    {
        if (rotation.elevationPivot == Entity.Null) return;

        float elevation = rotation.currentElevation;
        float speed = rotation.currentElevationSpeed;
        float targetElevation = targeting.elevationAim;

        // Delta angle
        float deltaAngle = targetElevation - elevation;
        deltaAngle = (deltaAngle + 180f) % 360f - 180f;

        // Check if target acquired
        targeting.isElevationRotationTarget = math.abs(deltaAngle) <= targeting.targetAcquiredAngle;

        // Accelerate/decelerate
        if (math.abs(deltaAngle) > 0.05f)
            speed += rotation.elevationRotationAcceleration * deltaTime;
        else
            speed -= rotation.elevationRotationAcceleration * deltaTime;

        speed = math.clamp(speed, 0f, rotation.elevationRotationSpeed);

        // Rotation step
        float rotationStep = speed * deltaTime;
        if (math.abs(deltaAngle) < rotationStep)
            elevation = targetElevation;
        else
            elevation += rotationStep * math.sign(deltaAngle);

        // Clamp if constrained
        if (rotation.elevationLimited)
        {
            elevation = math.clamp(elevation, rotation.minElevationLimit, rotation.maxElevationLimit);
            if (elevation <= rotation.minElevationLimit || elevation >= rotation.maxElevationLimit)
                speed = 0f;
        }

        // Save state
        rotation.currentElevation = elevation;
        rotation.currentElevationSpeed = speed;
        rotation.elevationSpeedFactor = math.abs(speed) / rotation.elevationRotationSpeed;
        rotation.IsElevationRotationSFX = rotation.elevationSpeedFactor > 0.05f;

        // Apply transform
        LocalTransform pivotTransform = pivotTransformLookup[rotation.elevationPivot];
        pivotTransform = pivotTransform.WithRotation(quaternion.Euler(math.radians(elevation), 0, 0));
        ecb.SetComponent<LocalTransform>(sortkey, rotation.elevationPivot, pivotTransform);
    }
}
