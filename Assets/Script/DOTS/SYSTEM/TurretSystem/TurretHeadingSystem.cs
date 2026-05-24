using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Xoay heading pivot theo targeting.headingAim.
/// Chỉ làm rotation — KHÔNG tính targeting.
/// </summary>
[UpdateAfter(typeof(TurretTargetingSystem))]
partial struct TurretHeadingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<TurretRotation, TurretTargeting>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretHeadingJob turretHeadingJob = new()
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            pivotTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            ecb = ecb.AsParallelWriter()
        };
        JobHandle jobHandle = turretHeadingJob.ScheduleParallel(state.Dependency);
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
public partial struct TurretHeadingJob : IJobEntity
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
        if (rotation.headingPivot == Entity.Null) return;

        float heading = rotation.currentHeading;
        float speed = rotation.currentHeadingSpeed;
        float targetHeading = targeting.headingAim;

        // normalize delta angle -180..180
        float deltaAngle = targetHeading - heading;
        deltaAngle = (deltaAngle + 180f) % 360f - 180f;

        // Check if target acquired
        targeting.isHeadingRotationTarget = math.abs(deltaAngle) <= targeting.targetAcquiredAngle;

        // Accelerate/decelerate
        if (math.abs(deltaAngle) > 0.05f)
            speed += rotation.headingRotationAcceleration * deltaTime;
        else
            speed -= rotation.headingRotationAcceleration * deltaTime;

        speed = math.clamp(speed, 0f, rotation.headingRotationSpeed);

        // Rotation step
        float rotationStep = speed * deltaTime;
        if (math.abs(deltaAngle) < rotationStep)
            heading = targetHeading;
        else
            heading += rotationStep * math.sign(deltaAngle);

        // Clamp if constrained
        if (rotation.headingLimited)
        {
            heading = math.clamp(heading, rotation.minHeadingLimit, rotation.maxHeadingLimit);
            if (heading <= rotation.minHeadingLimit || heading >= rotation.maxHeadingLimit)
                speed = 0f;
        }

        // Save state
        rotation.currentHeading = heading;
        rotation.currentHeadingSpeed = speed;
        rotation.headingSpeedFactor = math.abs(speed) / rotation.headingRotationSpeed;
        rotation.IsHeadingRotationSFX = rotation.headingSpeedFactor > 0.15f;

        // Apply transform
        LocalTransform pivotTransform = pivotTransformLookup[rotation.headingPivot];
        pivotTransform = pivotTransform.WithRotation(quaternion.Euler(0, math.radians(heading), 0));
        ecb.SetComponent<LocalTransform>(sortkey, rotation.headingPivot, pivotTransform);
    }
}
