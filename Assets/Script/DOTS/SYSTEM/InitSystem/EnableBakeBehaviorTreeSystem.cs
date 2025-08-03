using Opsive.BehaviorDesigner.Runtime;
using Unity.Burst;
using Unity.Entities;
[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct EnableBakeBehaviorTreeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        BehaviorTree.EnableBakedBehaviorTreeSystem(World.DefaultGameObjectInjectionWorld);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
