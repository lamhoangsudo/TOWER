using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
partial struct EnemyMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<LocalTransform> localTransform, RefRW<Enemy> target) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Enemy>>())
        {
            if(!target.ValueRO.test) continue;
            target.ValueRW.time -= SystemAPI.Time.DeltaTime;
            if(target.ValueRO.time <= 0f)
            {
                Unity.Mathematics.Random random = target.ValueRO.RandomGenerator;
                float3 targetPosition = new float3 (random.NextFloat(-50f, 50f), random.NextFloat(0f, 10f), 30f);
                target.ValueRW.TargetPosition = targetPosition;
                target.ValueRW.RandomGenerator = random;
                target.ValueRW.time = 1f;
            }
            localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, target.ValueRO.TargetPosition, SystemAPI.Time.DeltaTime * 0.1f);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
