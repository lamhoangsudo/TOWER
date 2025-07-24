using Unity.Burst;
using Unity.Entities;
partial struct LifeTimeExplosionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach((RefRW<Explosion> explosion, Entity entity) in SystemAPI.Query<RefRW<Explosion>>().WithEntityAccess())
        {
            explosion.ValueRW.lifeTime -= SystemAPI.Time.DeltaTime;
            if (explosion.ValueRO.lifeTime <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
