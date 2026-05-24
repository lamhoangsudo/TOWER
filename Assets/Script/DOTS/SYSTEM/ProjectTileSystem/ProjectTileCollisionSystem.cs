using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Raycast collision detection cho projectiles.
/// Dùng previousPosition → currentPosition để tránh tunneling.
/// Hit enemy → spawn explosion + destroy projectile.
/// Hit ground (missile) → spawn explosion + destroy.
/// Dùng EndSimulationEntityCommandBufferSystem để tránh sync point.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ProjectileMovementSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct ProjectileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Projectile, LocalTransform>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        ProjectileCollisionJob job = new()
        {
            collisionWorld = collisionWorld,
            ecb = ecb,
            DeltaTime = SystemAPI.Time.DeltaTime,
            enemyLookup = SystemAPI.GetComponentLookup<Enemy>(isReadOnly: true),
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct ProjectileCollisionJob : IJobEntity
{
    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public ComponentLookup<Enemy> enemyLookup;
    public EntityCommandBuffer.ParallelWriter ecb;
    public float DeltaTime;

    public void Execute(
        Entity entity,
        in Projectile projectile,
        in LocalTransform localTransform,
        [ChunkIndexInQuery] int sortkey)
    {
        // Anti-tunneling: raycast từ previousPosition → currentPosition
        float3 rayStart = projectile.previousPosition;
        float3 rayEnd = localTransform.Position;

        // Nếu previousPosition chưa được set (frame đầu tiên), fallback sang forward ray
        if (math.lengthsq(rayStart - rayEnd) < 1e-6f)
        {
            float rayLength = projectile.projectileCurrentSpeed * DeltaTime * 2f;
            rayStart = localTransform.Position;
            rayEnd = rayStart + localTransform.Forward() * rayLength;
        }

        CollisionFilter filter;
        switch (projectile.projectileType)
        {
            case Enum.ProjectileType.Bullet:
                filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << 3, // enemy layer
                    GroupIndex = 0,
                };
                break;
            case Enum.ProjectileType.Missile:
                filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = (1u << 3) | (1u << 7), // enemy + ground layer
                    GroupIndex = 0,
                };
                break;
            default:
                return;
        }

        RaycastInput raycastInput = new()
        {
            Start = rayStart,
            End = rayEnd,
            Filter = filter,
        };

        if (collisionWorld.CastRay(raycastInput, out RaycastHit hit))
        {
            // Spawn explosion tại hit point
            if (projectile.projectileExplosion != Entity.Null)
            {
                Entity explosion = ecb.Instantiate(sortkey, projectile.projectileExplosion);
                ecb.SetComponent(sortkey, explosion, LocalTransform.FromPosition(hit.Position));
            }

            // Destroy projectile
            ecb.DestroyEntity(sortkey, entity);
        }
    }
}
