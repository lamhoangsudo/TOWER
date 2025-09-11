using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MovementSystem : ISystem
{
    private quaternion targetQuaternion;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //foreach((RefRW<LocalTransform> localTransform, RefRO<Movement> movement, RefRW<Rotation> rotation) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Movement>, RefRW<Rotation>>())
        //{
        //    if(!movement.ValueRO.moveVector.Equals(float3.zero)) localTransform.ValueRW.Position += movement.ValueRO.moveSpeed * SystemAPI.Time.DeltaTime * movement.ValueRO.moveVector;
        //    rotation.ValueRW.pitch = math.clamp(rotation.ValueRO.pitch, rotation.ValueRO.pitchMin, rotation.ValueRO.pitchMax);
        //    targetQuaternion = quaternion.Euler(math.radians(rotation.ValueRO.pitch), math.radians(rotation.ValueRO.yaw), 0);
        //    if(math.distancesq(targetQuaternion.value, localTransform.ValueRO.Rotation.value) > 1e-6f)
        //    {
        //        localTransform.ValueRW = localTransform.ValueRO.WithRotation(targetQuaternion);
        //    }
        //}
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
