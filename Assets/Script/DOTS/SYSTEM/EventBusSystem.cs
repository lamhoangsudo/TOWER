using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
public partial struct EventBusSystem : ISystem
{
    private NativeArray<JobHandle> jobHandles;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        jobHandles = new NativeArray<JobHandle>(100, Allocator.Persistent);
    }
    public void OnUpdate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        jobHandles.Dispose();
    }
}
