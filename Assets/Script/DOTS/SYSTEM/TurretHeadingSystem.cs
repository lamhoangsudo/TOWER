using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

partial struct TurretHeadingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        #region old code
        /*
        foreach (RefRW<Turret> turret in SystemAPI.Query<RefRW<Turret>>())
        {
            // Bỏ qua nếu không có pivot
            if (turret.ValueRO.headingPivot == Entity.Null)
                continue;

            float heading = turret.ValueRW.currentHeading;
            float speed = turret.ValueRW.currentHeadingSpeed;
            float targetHeading;

            if (turret.ValueRO.target != Entity.Null)
            {
                if (
                    SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.target) &&
                    SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.headingPivot)
                )
                {
                    LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(turret.ValueRO.target);
                    LocalToWorld pivotTransform = SystemAPI.GetComponent<LocalToWorld>(turret.ValueRO.headingPivot);

                    float3 targetPos = targetTransform.Position;
                    float3 pivotPos = pivotTransform.Position;

                    float3 toTarget = targetPos - pivotPos;

                    targetHeading = math.degrees(math.atan2(toTarget.x, toTarget.z));
                    if ((targetHeading < turret.ValueRO.minHeadingLimit || targetHeading > turret.ValueRO.maxHeadingLimit) && turret.ValueRO.headingLimited)
                    {
                        targetHeading = heading;
                    }
                }
                else
                {
                    targetHeading = heading; // target không hợp lệ
                }
            }
            else
            {
                targetHeading = turret.ValueRO.resetOrientation ? 0f : heading;
            }

            // normalize delta angle -180..180
            float deltaAngle = targetHeading - heading;
            deltaAngle = (deltaAngle + 180f) % 360f - 180f;
            if (math.abs(deltaAngle) <= turret.ValueRO.targetAquiredAngle)
            {
                turret.ValueRW.isHeadingRotationTarget = true;
            }
            else
            {
                turret.ValueRW.isHeadingRotationTarget = false;
            }
            // tăng giảm tốc độ
            if (math.abs(deltaAngle) > 0.05f)
            {
                speed += turret.ValueRO.headingRotationAcceleration * deltaTime;
            }
            else
            {
                speed -= turret.ValueRO.headingRotationAcceleration * deltaTime;
            }

            speed = math.clamp(speed, 0f, turret.ValueRO.headingRotationSpeed);

            // bước xoay
            float rotationStep = speed * deltaTime;

            if (math.abs(deltaAngle) < rotationStep)
            {
                heading = targetHeading;
            }
            else
            {
                heading += rotationStep * math.sign(deltaAngle);
            }

            // clamp heading nếu cần
            if (turret.ValueRO.headingLimited)
            {
                heading = math.clamp(
                    heading,
                    turret.ValueRO.minHeadingLimit,
                    turret.ValueRO.maxHeadingLimit
                );
                if (heading <= turret.ValueRO.minHeadingLimit || heading >= turret.ValueRO.maxHeadingLimit)
                {
                    speed = 0f; // dừng xoay nếu chạm giới hạn
                }
            }

            // lưu state
            turret.ValueRW.currentHeading = heading;
            turret.ValueRW.currentHeadingSpeed = speed;
            turret.ValueRW.headingSpeedFactor = math.abs(speed) / turret.ValueRO.headingRotationSpeed;
            if (turret.ValueRO.headingSpeedFactor > 0.05f)
            {
                turret.ValueRW.IsHeadingRotationSFX = true;
            }
            else
            {
                turret.ValueRW.IsHeadingRotationSFX = false;
            }
            // apply transform
            if (SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.headingPivot))
            {
                RefRW<LocalTransform> pivotTransformRW =
                    SystemAPI.GetComponentRW<LocalTransform>(turret.ValueRO.headingPivot);

                pivotTransformRW.ValueRW = pivotTransformRW.ValueRW.WithRotation(
                    quaternion.Euler(0, math.radians(heading), 0)
                );
            }
        }
        */
        #endregion
        #region new code
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretHeadingJob turretHeadingJob = new()
        {
            deltaTime = deltaTime,
            targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            pivotLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            pivotTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            ecb = ecb.AsParallelWriter()
        };
        turretHeadingJob.ScheduleParallel();
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
public partial struct TurretHeadingJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public ComponentLookup<LocalTransform> targetTransformLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> pivotLocalToWorldLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> pivotTransformLookup;
    public EntityCommandBuffer.ParallelWriter ecb;
    public void Execute(
        ref Turret turret,
        [ChunkIndexInQuery] int sortkey
    )
    {
        // Bỏ qua nếu không có pivot
        if (turret.headingPivot == Entity.Null)
            return;

        float heading = turret.currentHeading;
        float speed = turret.currentHeadingSpeed;
        float targetHeading;

        if (turret.target != Entity.Null)
        {
            LocalTransform targetTransform = targetTransformLookup[turret.target];
            LocalToWorld pivotLocalToWorld = pivotLocalToWorldLookup[turret.headingPivot];

            float3 targetPos = targetTransform.Position;
            float3 pivotPos = pivotLocalToWorld.Position;

            float3 toTarget = targetPos - pivotPos;

            targetHeading = math.degrees(math.atan2(toTarget.x, toTarget.z));
            if ((targetHeading < turret.minHeadingLimit || targetHeading > turret.maxHeadingLimit) && turret.headingLimited)
            {
                targetHeading = heading;
            }
        }
        else
        {
            targetHeading = turret.resetOrientation ? 0f : heading;
        }

        // normalize delta angle -180..180
        float deltaAngle = targetHeading - heading;
        deltaAngle = (deltaAngle + 180f) % 360f - 180f;
        if (math.abs(deltaAngle) <= turret.targetAquiredAngle)
        {
            turret.isHeadingRotationTarget = true;
        }
        else
        {
            turret.isHeadingRotationTarget = false;
        }
        // tăng giảm tốc độ
        if (math.abs(deltaAngle) > 0.05f)
        {
            speed += turret.headingRotationAcceleration * deltaTime;
        }
        else
        {
            speed -= turret.headingRotationAcceleration * deltaTime;
        }

        speed = math.clamp(speed, 0f, turret.headingRotationSpeed);

        // bước xoay
        float rotationStep = speed * deltaTime;

        if (math.abs(deltaAngle) < rotationStep)
        {
            heading = targetHeading;
        }
        else
        {
            heading += rotationStep * math.sign(deltaAngle);
        }

        // clamp heading nếu cần
        if (turret.headingLimited)
        {
            heading = math.clamp(
                heading,
                turret.minHeadingLimit,
                turret.maxHeadingLimit
            );
            if (heading <= turret.minHeadingLimit || heading >= turret.maxHeadingLimit)
            {
                speed = 0f; // dừng xoay nếu chạm giới hạn
            }
        }

        // lưu state
        turret.currentHeading = heading;
        turret.currentHeadingSpeed = speed;
        turret.headingSpeedFactor = math.abs(speed) / turret.headingRotationSpeed;
        if (turret.headingSpeedFactor > 0.05f)
        {
            turret.IsHeadingRotationSFX = true;
        }
        else
        {
            turret.IsHeadingRotationSFX = false;
        }
        // apply transform
        LocalTransform pivotTransformWriter = pivotTransformLookup[turret.headingPivot];
        pivotTransformWriter = pivotTransformWriter.WithRotation(
            quaternion.Euler(0, math.radians(heading), 0)
        );
        ecb.SetComponent<LocalTransform>(sortkey, turret.headingPivot, pivotTransformWriter);
    }
}