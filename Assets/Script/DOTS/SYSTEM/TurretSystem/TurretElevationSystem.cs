using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

partial struct TurretElevationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        #region new code
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretElevationJob turretPitchJob = new TurretElevationJob
        {
            deltaTime = deltaTime,
            targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            pivotLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            pivotTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            ecb = ecb.AsParallelWriter()
        };
        JobHandle jobHandle = turretPitchJob.ScheduleParallel(state.Dependency);
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
public partial struct TurretElevationJob : IJobEntity
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
        if (turret.elevationPivot == Entity.Null)
            return;

        float elevation = turret.currentElevation;
        float speed = turret.currentElevationSpeed;
        float targetElevation;

        if (turret.target != Entity.Null)
        {
            LocalTransform targetTransform = targetTransformLookup[turret.target];
            LocalToWorld pivotLocalToWorld = pivotLocalToWorldLookup[turret.elevationPivot];

            float3 targetPos = targetTransform.Position;
            float3 pivotPos = pivotLocalToWorld.Position;

            float3 toTarget = pivotPos - targetPos;

            float distanceXZ = math.distance(targetPos, pivotPos);
            targetElevation = math.degrees(math.atan2(toTarget.y, distanceXZ));
            if ((targetElevation < turret.minElevationLimit || targetElevation > turret.maxElevationLimit) && turret.elevationLimited)
            {
                targetElevation = elevation;
            }
        }
        else
        {
            targetElevation = turret.resetOrientation ? 0f : elevation;
        }

        // delta angle (elevation)

        float deltaAngle = targetElevation - elevation;
        deltaAngle = (deltaAngle + 180f) % 360f - 180f;
        if (math.abs(deltaAngle) <= turret.targetAquiredAngle)
        {
            turret.isElevationRotationTarget = true;
        }
        else
        {
            turret.isElevationRotationTarget = false;
        }
        // tăng giảm tốc độ
        if (math.abs(deltaAngle) > 0.05f)
        {
            speed += turret.elevationRotationAcceleration * deltaTime;
        }
        else
        {
            speed -= turret.elevationRotationAcceleration * deltaTime;
        }

        speed = math.clamp(speed, 0f, turret.elevationRotationSpeed);

        // bước nâng
        float rotationStep = speed * deltaTime;

        if (math.abs(deltaAngle) < rotationStep)
        {
            elevation = targetElevation;
        }
        else
        {
            elevation += rotationStep * math.sign(deltaAngle);
        }

        // clamp pitch nếu cần
        if (turret.elevationLimited)
        {
            elevation = math.clamp(
                elevation,
                turret.minElevationLimit,
                turret.maxElevationLimit
            );
            if (elevation <= turret.minElevationLimit || elevation >= turret.maxElevationLimit)
            {
                speed = 0f; // dừng xoay nếu chạm giới hạn
            }
        }

        // lưu state
        turret.currentElevation = elevation;
        turret.currentElevationSpeed = speed;
        turret.elevationSpeedFactor = math.abs(speed) / turret.elevationRotationSpeed;
        if (turret.elevationSpeedFactor > 0.05f)
        {
            turret.IsElevationRotationSFX = true;
        }
        else
        {
            turret.IsElevationRotationSFX = false;
        }
        // apply transform

        LocalTransform pivotTransformWriter = pivotTransformLookup[turret.elevationPivot];

        pivotTransformWriter = pivotTransformWriter.WithRotation(
            quaternion.Euler(math.radians(elevation), 0, 0)
        );
        ecb.SetComponent<LocalTransform>(sortkey, turret.elevationPivot, pivotTransformWriter);
    }
}
