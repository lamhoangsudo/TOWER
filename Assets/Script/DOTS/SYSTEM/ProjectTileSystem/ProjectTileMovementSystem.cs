using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
partial struct ProjectTileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<LocalTransform> localTransform, RefRW<ProjecTile> projecTile) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<ProjecTile>>())
        {
            switch(projecTile.ValueRO.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    localTransform.ValueRW.Position += projecTile.ValueRO.projecTileCurrentSpeed * SystemAPI.Time.DeltaTime * localTransform.ValueRO.Forward();
                    break;
                case Enum.ProjectTileType.Missile:
                    break;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
