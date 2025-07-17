using Unity.Burst;
using Unity.Entities;
partial struct ProjectTileLifeTimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        ProjectTileLifeTimeJob projectTileLifeTimeJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter()
        };
        projectTileLifeTimeJob.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
    [BurstCompile]
    public partial struct ProjectTileLifeTimeJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute([ChunkIndexInQuery] int sortkey,ref ProjecTile projecTile, Entity entity)
        {
            projecTile.projecTileCurrentLifetime -= DeltaTime;
            if (projecTile.projecTileCurrentLifetime > 0f) return;
            ecb.DestroyEntity(sortkey, entity);
        }
    }
}
