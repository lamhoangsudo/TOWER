using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct TargetMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<LocalTransform> localTransform, RefRW<Target> target) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Target>>())
        {
            target.ValueRW.time -= SystemAPI.Time.DeltaTime;
            if(target.ValueRO.time <= 0f)
            {
                Unity.Mathematics.Random random = target.ValueRO.RandomGenerator;
                float3 targetPosition = random.NextFloat3(new float3(-20f, 5, 0f), new float3(20f, 10f, 0f));
                target.ValueRW.TargetPosition = targetPosition;
                target.ValueRW.RandomGenerator = random;
                target.ValueRW.time = 10f;
            }
            localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, target.ValueRO.TargetPosition, SystemAPI.Time.DeltaTime * 2f);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
