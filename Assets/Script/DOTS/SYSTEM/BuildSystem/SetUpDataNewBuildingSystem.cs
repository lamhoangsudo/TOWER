using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(BuildTimeSystem))]
public partial struct SetUpDataNewBuildingSystem : ISystem
{
    public EntityCommandBuffer ecb_SetUpDataNewBuildingJob;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ecb_SetUpDataNewBuildingJob = new EntityCommandBuffer(Allocator.TempJob);
        //ecb_SetUpDataNewBuildingJob = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        SetUpDataNewBuildingJob setUpDataNewBuildingJob = new()
        {
            ecb = ecb_SetUpDataNewBuildingJob.AsParallelWriter(),
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
            componentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            SnapPointsBuffersLookup = SystemAPI.GetBufferLookup<SnapPointBuffer>(isReadOnly: true),
            SnapPointsDirectionBuffersLookup = SystemAPI.GetBufferLookup<SnapPointsDirectionBuffer>(isReadOnly: true),
        };
        JobHandle jobHandleSetUpDataNewBuildingJob = setUpDataNewBuildingJob.ScheduleParallel(state.Dependency);
        jobHandleSetUpDataNewBuildingJob.Complete();
        ecb_SetUpDataNewBuildingJob.Playback(state.EntityManager);
        ecb_SetUpDataNewBuildingJob.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
[BurstCompile]
public partial struct SetUpDataNewBuildingJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public BufferLookup<SnapPointBuffer> SnapPointsBuffersLookup;
    [ReadOnly] public BufferLookup<SnapPointsDirectionBuffer> SnapPointsDirectionBuffersLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> componentLocalToWorldLookup;
    [ReadOnly] public CollisionWorld collisionWorld;
    public void Execute([ChunkIndexInQuery] int sortkey, ref BuildingGhost buildingGhost, in IsBuilding isBuilding, in LocalTransform localTransform, Entity entity)
    {
        if (isBuilding.buildingEntity == Entity.Null) return;
        DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers = SnapPointsDirectionBuffersLookup[entity];
        DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBufferBuildings = SnapPointsDirectionBuffersLookup[isBuilding.buildingEntity];
        for (int i = 0; i < snapPointsDirectionBuffers.Length; i++)
        {
            DynamicBuffer<SnapPointBuffer> snapPointBuffers = SnapPointsBuffersLookup[snapPointsDirectionBuffers[i].SnapPointsDirectionEntity];
            DynamicBuffer<SnapPointBuffer> snapPointBufferBuildings = ecb.SetBuffer<SnapPointBuffer>(sortkey, snapPointsDirectionBufferBuildings[i].SnapPointsDirectionEntity);
            for (int j = 0; j < snapPointBuffers.Length; j++)
            {
                snapPointBufferBuildings.Add(new SnapPointBuffer
                {
                    snapPointPosition = componentLocalToWorldLookup[snapPointBuffers[j].snapPointEntity].Position,
                    snapPointType = snapPointBuffers[j].snapPointType,
                    isOccupied = false,
                    distanceSnapPointToBuildingGhost = 0f,
                    offset = math.distance(componentLocalToWorldLookup[snapPointBuffers[j].snapPointEntity].Position, componentLocalToWorldLookup[entity].Position),
                });
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
                MyCollector.IgnoreEntityCollector ignoreEntityCollector = new(entity, 1.0f, Allocator.TempJob);
                if (collisionWorld.CastRay(raycastInput, ref ignoreEntityCollector))
                {
                    if (ignoreEntityCollector.Hits.Length > 0)
                    {
                        ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, ignoreEntityCollector.Hits[0].Entity, true);
                    }
                }
            }
        }
        ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, isBuilding.buildingEntity, true);
        ecb.DestroyEntity(sortkey, entity);
    }
}
