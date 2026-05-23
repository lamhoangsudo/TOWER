using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Raycast collision detection cho projectiles.
/// Raycast mỗi frame theo hướng di chuyển.
/// Hit enemy → spawn explosion + destroy projectile.
/// Hit ground (missile) → spawn explosion + destroy.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ProjectTileMovementSystem))]
partial struct ProjectTileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ProjecTile, LocalTransform>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        EntityCommandBuffer ecb = new(Allocator.TempJob);

        ProjectTileCollisionJob job = new()
        {
            collisionWorld = collisionWorld,
            ecb = ecb.AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            enemyLookup = SystemAPI.GetComponentLookup<Enemy>(isReadOnly: true),
        };
        job.ScheduleParallel();
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct ProjectTileCollisionJob : IJobEntity
{
    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public ComponentLookup<Enemy> enemyLookup;
    public EntityCommandBuffer.ParallelWriter ecb;
    public float DeltaTime;

    public void Execute(
        Entity entity,
        in ProjecTile projectile,
        in LocalTransform localTransform,
        [ChunkIndexInQuery] int sortkey)
    {
        // Raycast forward theo tốc độ hiện tại
        float rayLength = projectile.projecTileCurrentSpeed * DeltaTime * 2f; // x2 để bù frame timing
        float3 rayStart = localTransform.Position;
        float3 rayEnd = rayStart + localTransform.Forward() * rayLength;

        CollisionFilter filter;
        switch (projectile.projectTileType)
        {
            case Enum.ProjectTileType.Bullet:
                filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << 3, // enemy layer
                    GroupIndex = 0,
                };
                break;
            case Enum.ProjectTileType.Missile:
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
