using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
partial struct LifeTimeExplosionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        LifeTimeExplosionJob lifeTimeExplosionJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
        };
        JobHandle lifeTimeExplosionJobHandle = lifeTimeExplosionJob.ScheduleParallel(state.Dependency);
        lifeTimeExplosionJobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
    [BurstCompile]
    public partial struct LifeTimeExplosionJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float DeltaTime;
        public void Execute([ChunkIndexInQuery] int sortkey, ref Explosion explosion, Entity entity)
        {
            explosion.lifeTime -= DeltaTime;
            if (explosion.lifeTime <= 0)
            {
                ecb.DestroyEntity(sortkey, entity);
            }
        }
    }
}
