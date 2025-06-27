using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
public partial struct EventBusSystem : ISystem
{
    private NativeArray<JobHandle> jobHandles;
    private NativeList<Entity> onProjecTileEntityHitList;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        jobHandles = new NativeArray<JobHandle>(100, Allocator.Persistent);
        onProjecTileEntityHitList = new NativeList<Entity>(Allocator.Persistent);
    }
    public void OnUpdate(ref SystemState state)
    {
        onProjecTileEntityHitList.Clear();
        jobHandles[0] = new ResetEventProjecTileHitIJob
        {
            onProjecTileEntityHitList = onProjecTileEntityHitList.AsParallelWriter()
        }.Schedule(SystemAPI.QueryBuilder().WithAll<ProjecTile>().Build(), state.Dependency);
        EventBusMono.Instance?.TriggerOnProjecTileHit(onProjecTileEntityHitList);
        state.Dependency = JobHandle.CombineDependencies(jobHandles);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        jobHandles.Dispose();
    }
}
public partial struct ResetEventProjecTileHitIJob : IJobEntity
{
    public NativeList<Entity>.ParallelWriter onProjecTileEntityHitList;
    public void Execute(ref ProjecTile projecTile, Entity entity)
    {
        onProjecTileEntityHitList.AddNoResize(entity);
        projecTile.isHit = false;
    }
}
