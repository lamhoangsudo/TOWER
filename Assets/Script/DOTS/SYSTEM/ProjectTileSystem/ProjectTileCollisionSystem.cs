using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
partial struct ProjectTileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<ProjecTile> projecTile, Entity entity) in SystemAPI.Query<RefRW<ProjecTile>>().WithEntityAccess())
        {
            switch(projecTile.ValueRO.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    if (projecTile.ValueRO.timeDelayRayMax == 0)
                    {
                        projecTile.ValueRW.timeDelayRayMax = projecTile.ValueRO.targetDistance / projecTile.ValueRO.projecTileCurrentSpeed;
                        projecTile.ValueRW.timeDelayRay = projecTile.ValueRW.timeDelayRayMax;
                    }
                    projecTile.ValueRW.timeDelayRay -= SystemAPI.Time.DeltaTime;
                    if (projecTile.ValueRO.timeDelayRay > 0) continue;
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
