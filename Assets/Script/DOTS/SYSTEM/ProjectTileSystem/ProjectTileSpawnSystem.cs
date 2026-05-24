using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Spawn projectile entity khi BarrelFireEffectSystem set ProjectileSpawnShoot.isSpawner = true.
/// Đọc spawn position từ LocalToWorld của point shoot entity.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(BarrelFireEffectSystem))]
partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ProjectileSpawnShoot, LocalToWorld>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);

        ProjectileSpawnJob job = new()
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
public partial struct ProjectileSpawnJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        ref ProjectileSpawnShoot spawnShoot,
        in LocalToWorld localToWorld,
        [ChunkIndexInQuery] int sortkey)
    {
        if (!spawnShoot.isSpawner) return;

        // Build projectile data
        Projectile projectileData = new()
        {
            projectileLifetimeMax = spawnShoot.projectileLifetimeMax,
            projectileCurrentLifetime = spawnShoot.projectileLifetimeMax,
            projectileExplosion = spawnShoot.entityProjectileExplosion,
            targetDistance = math.distance(localToWorld.Position, spawnShoot.targetPosition),
            previousPosition = localToWorld.Position,
        };

        // Set type-specific data
        switch (spawnShoot.firingPattern)
        {
            case Enum.WeaponFiringPattern.MissileLauncher:
                projectileData.projectileType = Enum.ProjectileType.Missile;
                projectileData.homingTarget = spawnShoot.homingTarget;
                projectileData.homingSpeed = 10f;
                projectileData.projectileAcceleration = spawnShoot.projectileAcceleration;
                projectileData.projectileCurrentSpeed = spawnShoot.projectileStartSpeed;
                projectileData.projectileMaxSpeed = spawnShoot.projectileMaxSpeed;
                break;
            default:
                projectileData.projectileType = Enum.ProjectileType.Bullet;
                projectileData.projectileMaxSpeed = spawnShoot.projectileMaxSpeed;
                projectileData.projectileCurrentSpeed = spawnShoot.projectileMaxSpeed;
                break;
        }

        // Spawn
        Entity projectileEntity = ecb.Instantiate(sortkey, spawnShoot.entityProjectilePrefab);
        ecb.SetComponent(sortkey, projectileEntity, projectileData);
        ecb.SetComponent(sortkey, projectileEntity, LocalTransform.FromPositionRotation(localToWorld.Position, localToWorld.Rotation));

        // Reset trigger
        spawnShoot.isSpawner = false;
    }
}
