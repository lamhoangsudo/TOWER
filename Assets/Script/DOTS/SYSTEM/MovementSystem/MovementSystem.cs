using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<LocalTransform> localTransform, RefRO<Movement> movement) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Movement>>())
        {
            localTransform.ValueRW.Position.xz += movement.ValueRO.moveVector * movement.ValueRO.moveSpeed * SystemAPI.Time.DeltaTime;
            if(math.lengthsq(movement.ValueRO.moveVector) > float.Epsilon)
            {
                localTransform.ValueRW.Rotation = quaternion.LookRotationSafe(new float3(movement.ValueRO.moveVector.x, 0, movement.ValueRO.moveVector.y), math.up());
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
