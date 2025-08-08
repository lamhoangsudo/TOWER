using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

partial struct TurretHeadingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Turret>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        #region new code
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretHeadingJob turretHeadingJob = new()
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            pivotLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            pivotTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            ecb = ecb.AsParallelWriter()
        };
        JobHandle jobHandle = turretHeadingJob.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
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