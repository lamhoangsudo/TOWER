using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
partial struct ProjectTileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach ((RefRW<ProjecTile> projecTile, RefRO<LocalTransform> localTransform, Entity entity) in SystemAPI.Query<RefRW<ProjecTile>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            switch (projecTile.ValueRO.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    if (projecTile.ValueRO.timeDelayRayMax == 0)
                    {
                        projecTile.ValueRW.timeDelayRayMax = projecTile.ValueRO.targetDistance / (projecTile.ValueRO.projecTileCurrentSpeed * 10f);
                        projecTile.ValueRW.timeDelayRay = projecTile.ValueRW.timeDelayRayMax;
                    }
                    projecTile.ValueRW.timeDelayRay -= SystemAPI.Time.DeltaTime;
                    if (projecTile.ValueRO.timeDelayRay > 0) continue;
                    CollisionFilter collisionFilter = new()
                    {
                        BelongsTo = ~0u,
                        CollidesWith = ~0u,
                        GroupIndex = 0,
                    };
                    RaycastInput raycastInput = new()
                    {
                        Start = localTransform.ValueRO.Position,
                        End = localTransform.ValueRO.Position + projecTile.ValueRO.projecTileCurrentSpeed * SystemAPI.Time.DeltaTime * localTransform.ValueRO.Forward(),
                        Filter = collisionFilter,
                    };
                    UnityEngine.Debug.DrawRay(localTransform.ValueRO.Position, localTransform.ValueRO.Position + projecTile.ValueRO.projecTileCurrentSpeed * SystemAPI.Time.DeltaTime * localTransform.ValueRO.Forward(), Color.green);
                    if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
                    {
                        if(SystemAPI.HasComponent<Target>(raycastHit.Entity))
                        {
                            ecb.DestroyEntity(entity);
                        }
                    }
                    projecTile.ValueRW.timeDelayRay = projecTile.ValueRW.timeDelayRayMax;
                    break;
                case Enum.ProjectTileType.Missile:
                    projecTile.ValueRW.timeDelayRayMax = projecTile.ValueRO.targetDistance / projecTile.ValueRO.projecTileCurrentSpeed;
                    if (projecTile.ValueRO.timeDelayRay <= 0) projecTile.ValueRW.timeDelayRay = projecTile.ValueRO.timeDelayRayMax;
                    projecTile.ValueRW.timeDelayRay -= SystemAPI.Time.DeltaTime;
                    if (projecTile.ValueRO.timeDelayRay > 0) continue;
                    projecTile.ValueRW.timeDelayRay = projecTile.ValueRO.timeDelayRayMax;
                    break;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
