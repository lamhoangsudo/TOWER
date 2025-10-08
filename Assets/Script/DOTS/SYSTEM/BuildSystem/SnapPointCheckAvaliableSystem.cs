using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
//[UpdateAfter(typeof(SetUpDataNewBuildingSystem))]
public partial struct SnapPointCheckAvaliableSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        //CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        //SnapPointCheckAvaliableJob snapPointCheckAvaliableJob = new()
        //{
        //    ecb = ecb.AsParallelWriter(),
        //    collisionWorld = collisionWorld,
        //    snapPointBufferLookUp = SystemAPI.GetBufferLookup<SnapPointBuffer>(isReadOnly: true),
        //    snapPointsDirectionBufferLookUp = SystemAPI.GetBufferLookup<SnapPointsDirectionBuffer>(isReadOnly: true),
        //};
        //snapPointCheckAvaliableJob.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
[BurstCompile]
public partial struct SnapPointCheckAvaliableJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public BufferLookup<SnapPointsDirectionBuffer> snapPointsDirectionBufferLookUp;
    [ReadOnly] public BufferLookup<SnapPointBuffer> snapPointBufferLookUp;
    [ReadOnly] public CollisionWorld collisionWorld;
    public void Execute([ChunkIndexInQuery] int sortkey, in IsCheckSnapPoint isCheckSnapPoint, Entity entity)
    {
        DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers = snapPointsDirectionBufferLookUp[entity];
        for (int i = 0; i < snapPointsDirectionBuffers.Length; i++)
        {
            DynamicBuffer<SnapPointBuffer> snapPointBuffers = snapPointBufferLookUp[snapPointsDirectionBuffers[i].SnapPointsDirectionEntity];
            DynamicBuffer<SnapPointBuffer> snapPointBuffersWritter = ecb.SetBuffer<SnapPointBuffer>(sortkey, snapPointsDirectionBuffers[i].SnapPointsDirectionEntity);
            for (int j = 0; j < snapPointBuffers.Length; j++)
            {
                SnapPointBuffer snapPointBuffer = snapPointBuffers[j];
                RaycastInput raycastInput = new()
                {
                    Start = snapPointBuffers[j].snapPointPosition,
                    End = snapPointBuffers[j].snapPointPosition + snapPointsDirectionBuffers[i].directionVector * 0.1f,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = 1u << 8,
                        GroupIndex = 0,
                    }
                };
                bool check = false;
                MyCollector.IgnoreEntityCollector ignoreEntityCollector = new(entity, 1.0f, Allocator.TempJob);
                if (collisionWorld.CastRay(raycastInput, ref ignoreEntityCollector))
                {
                    if (ignoreEntityCollector.Hits.Length > 0)
                    {
                        check = true;
                    }
                }
                else
                {
                    check = false;
                }
                if (snapPointBuffer.isOccupied != check)
                {
                    snapPointBuffer.isOccupied = check;
                    //snapPointBuffers[j] = snapPointBuffer;
                }
                snapPointBuffersWritter.Add(snapPointBuffer);
            }
        }
        ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, entity, false);
    }
}