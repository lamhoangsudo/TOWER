using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Radar system: tìm enemy gần nhất trong range → set TurretTargeting.target.
/// Thay thế Opsive BehaviorTree (FindAllTarget + ChooseBestTarget).
/// Chạy TRƯỚC TurretTargetingSystem.
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(TurretTargetingSystem))]
public partial struct TurretRadarSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<RadarRangeRay, TurretTargeting, LocalTransform>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        TurretRadarJob job = new()
        {
            physicsWorld = physicsWorld,
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
        };
        job.Schedule();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct TurretRadarJob : IJobEntity
{
    [ReadOnly] public PhysicsWorldSingleton physicsWorld;
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;

    public void Execute(
        ref TurretTargeting targeting,
        in RadarRangeRay radar,
        in LocalTransform turretTransform)
    {
        // OverlapSphere để tìm tất cả entities trong range
        NativeList<DistanceHit> hits = new(Allocator.Temp);

        physicsWorld.OverlapSphere(
            turretTransform.Position,
            radar.radarScanRange,
            ref hits,
            new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = radar.enemyLayer,
                GroupIndex = 0,
            });

        if (hits.Length == 0)
        {
            targeting.target = Entity.Null;
            hits.Dispose();
            return;
        }

        // Chọn enemy gần nhất
        Entity closestEntity = Entity.Null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].Distance < closestDistance)
            {
                closestDistance = hits[i].Distance;
                closestEntity = hits[i].Entity;
            }
        }

        targeting.target = closestEntity;
        hits.Dispose();
    }
}
