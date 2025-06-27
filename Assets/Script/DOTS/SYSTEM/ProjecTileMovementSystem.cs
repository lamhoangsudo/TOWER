using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
/// <summary>
/// Handles moving the projectile forward based on its direction and speed,
/// and destroys it after its lifetime expires.
/// </summary>
partial struct ProjecTileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach ((RefRW<ProjecTile> projectile, RefRW<LocalTransform> transform, Entity entity) in
                 SystemAPI.Query<RefRW<ProjecTile>, RefRW<LocalTransform>>()
                 .WithEntityAccess())
        {
            transform.ValueRW.Position += projectile.ValueRO.direction * projectile.ValueRO.speed * deltaTime;
            projectile.ValueRW.lifetime -= deltaTime;
            if (projectile.ValueRW.lifetime <= 0f)
            {
                state.EntityManager.DestroyEntity(entity);
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
