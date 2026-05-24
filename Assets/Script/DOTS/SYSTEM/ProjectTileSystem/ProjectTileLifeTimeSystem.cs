using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Countdown lifetime → destroy khi hết.
/// Dùng EndSimulationEntityCommandBufferSystem để tránh sync point mỗi frame.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ProjectileCollisionSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct ProjectileLifeTimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Projectile>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        ProjectileLifeTimeJob job = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb,
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct ProjectileLifeTimeJob : IJobEntity
{
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(Entity entity, ref Projectile projectile, [ChunkIndexInQuery] int sortkey)
    {
        projectile.projectileCurrentLifetime -= DeltaTime;
        if (projectile.projectileCurrentLifetime <= 0f)
        {
            ecb.DestroyEntity(sortkey, entity);
        }
    }
}
