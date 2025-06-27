using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

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
            // Di chuyển theo hướng
            transform.ValueRW.Position +=
                projectile.ValueRO.direction * projectile.ValueRO.speed * deltaTime;

            // Giảm lifetime
            projectile.ValueRW.lifetime -= deltaTime;

            // Hết thời gian sống thì destroy
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
