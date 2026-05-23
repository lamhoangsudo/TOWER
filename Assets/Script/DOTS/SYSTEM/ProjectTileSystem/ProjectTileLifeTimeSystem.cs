using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Countdown lifetime → destroy khi hết.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(ProjectTileCollisionSystem))]
partial struct ProjectTileLifeTimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ProjecTile>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);

        ProjectTileLifeTimeJob job = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
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
public partial struct ProjectTileLifeTimeJob : IJobEntity
{
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(Entity entity, ref ProjecTile projectile, [ChunkIndexInQuery] int sortkey)
    {
        projectile.projecTileCurrentLifetime -= DeltaTime;
        if (projectile.projecTileCurrentLifetime <= 0f)
        {
            ecb.DestroyEntity(sortkey, entity);
        }
    }
}
