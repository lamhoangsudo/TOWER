using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Spawn projectile entity khi BarrelFireEffectSystem set ProjectTileSpawnShoot.isSpawner = true.
/// Đọc spawn position từ LocalToWorld của point shoot entity.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(BarrelFireEffectSystem))]
partial struct ProjectTileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ProjectTileSpawnShoot, LocalToWorld>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);

        ProjectTileSpawnJob job = new()
        {
            ecb = ecb.AsParallelWriter(),
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
public partial struct ProjectTileSpawnJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        ref ProjectTileSpawnShoot spawnShoot,
        in LocalToWorld localToWorld,
        [ChunkIndexInQuery] int sortkey)
    {
        if (!spawnShoot.isSpawner) return;

        // Build projectile data
        ProjecTile projectileData = new()
        {
            projecTileLifetimeMax = spawnShoot.projectileLifetimeMax,
            projecTileCurrentLifetime = spawnShoot.projectileLifetimeMax,
            projectileExplosion = spawnShoot.entityProjectTileExplosion,
            targetDistance = math.distance(localToWorld.Position, spawnShoot.targetPosition),
        };

        // Set type-specific data
        switch (spawnShoot.firingPattern)
        {
            case Enum.WeaponFiringPattern.MissileLauncher:
                projectileData.projectTileType = Enum.ProjectTileType.Missile;
                projectileData.homingTarget = spawnShoot.homingTarget;
                projectileData.homingSpeed = 10f;
                projectileData.projecTileAcceleration = spawnShoot.projectileAcceleration;
                projectileData.projecTileCurrentSpeed = spawnShoot.projectileStartSpeed;
                projectileData.projecTileMaxSpeed = spawnShoot.projectileMaxSpeed;
                break;
            default:
                projectileData.projectTileType = Enum.ProjectTileType.Bullet;
                projectileData.projecTileMaxSpeed = spawnShoot.projectileMaxSpeed;
                projectileData.projecTileCurrentSpeed = spawnShoot.projectileMaxSpeed;
                break;
        }

        // Spawn
        Entity projectileEntity = ecb.Instantiate(sortkey, spawnShoot.entityProjectTilePrefab);
        ecb.SetComponent(sortkey, projectileEntity, projectileData);
        ecb.SetComponent(sortkey, projectileEntity, LocalTransform.FromPositionRotation(localToWorld.Position, localToWorld.Rotation));

        // Reset trigger
        spawnShoot.isSpawner = false;
    }
}
