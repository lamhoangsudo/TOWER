using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Tính heading/elevation aim angles từ target position.
/// Chạy TRƯỚC rotation systems.
/// Nếu không có target → aim = 0 (reset) hoặc giữ nguyên.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(TurretHeadingSystem))]
[UpdateBefore(typeof(TurretElevationSystem))]
public partial struct TurretTargetingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<TurretRotation, TurretTargeting>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        TurretTargetingJob job = new()
        {
            targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            pivotLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
        };
        job.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct TurretTargetingJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> targetTransformLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> pivotLocalToWorldLookup;

    public void Execute(ref TurretTargeting targeting, in TurretRotation rotation)
    {
        if (targeting.target == Entity.Null)
        {
            if (targeting.resetOrientation)
            {
                targeting.headingAim = 0f;
                targeting.elevationAim = 0f;
            }
            else
            {
                targeting.headingAim = rotation.currentHeading;
                targeting.elevationAim = rotation.currentElevation;
            }
            return;
        }

        // Lấy target position
        LocalTransform targetTransform = targetTransformLookup[targeting.target];
        float3 targetPos = targetTransform.Position;

        // === Heading aim (yaw) ===
        if (rotation.headingPivot != Entity.Null)
        {
            LocalToWorld headingPivotWorld = pivotLocalToWorldLookup[rotation.headingPivot];
            float3 toTarget = targetPos - headingPivotWorld.Position;
            float headingAim = math.degrees(math.atan2(toTarget.x, toTarget.z));

            // Clamp nếu ngoài giới hạn
            if (rotation.headingLimited)
            {
                if (headingAim < rotation.minHeadingLimit || headingAim > rotation.maxHeadingLimit)
                {
                    headingAim = rotation.currentHeading; // giữ nguyên
                }
            }
            targeting.headingAim = headingAim;
        }

        // === Elevation aim (pitch) ===
        if (rotation.elevationPivot != Entity.Null)
        {
            LocalToWorld elevationPivotWorld = pivotLocalToWorldLookup[rotation.elevationPivot];
            float3 toTarget = elevationPivotWorld.Position - targetPos;
            float distanceXZ = math.distance(targetPos, elevationPivotWorld.Position);
            float elevationAim = math.degrees(math.atan2(toTarget.y, distanceXZ));

            // Clamp nếu ngoài giới hạn
            if (rotation.elevationLimited)
            {
                if (elevationAim < rotation.minElevationLimit || elevationAim > rotation.maxElevationLimit)
                {
                    elevationAim = rotation.currentElevation; // giữ nguyên
                }
            }
            targeting.elevationAim = elevationAim;
        }
    }
}
