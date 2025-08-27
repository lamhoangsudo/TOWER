using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial struct BuildTimeSystem : ISystem
{
    public EntityCommandBuffer ecb_BuildTimeSystemJob;
    public EntityCommandBuffer ecb_SetUpDataNewBuildingJob;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ecb_BuildTimeSystemJob = new EntityCommandBuffer(Allocator.TempJob);
        BuildTimeSystemJob buildTimeSystemJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb_BuildTimeSystemJob.AsParallelWriter(),
            //collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
            //componentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            //SnapPointsBuffersLookup = SystemAPI.GetBufferLookup<SnapPointBuffer>(isReadOnly: true),
            //SnapPointsDirectionBuffersLookup = SystemAPI.GetBufferLookup<SnapPointsDirectionBuffer>(isReadOnly: true),
        };
        JobHandle jobHandleBuildTimeSystemJob  = buildTimeSystemJob.ScheduleParallel(state.Dependency);
        jobHandleBuildTimeSystemJob.Complete();
        ecb_BuildTimeSystemJob.Playback(state.EntityManager);
        ecb_BuildTimeSystemJob.Dispose();

        ecb_SetUpDataNewBuildingJob = new EntityCommandBuffer(Allocator.TempJob);
        SetUpDataNewBuildingJob setUpDataNewBuildingJob = new()
        {
            ecb = ecb_SetUpDataNewBuildingJob.AsParallelWriter(),
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
            componentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            SnapPointsBuffersLookup = SystemAPI.GetBufferLookup<SnapPointBuffer>(isReadOnly: true),
            SnapPointsDirectionBuffersLookup = SystemAPI.GetBufferLookup<SnapPointsDirectionBuffer>(isReadOnly: true),
        };
        JobHandle jobHandleSetUpDataNewBuildingJob = setUpDataNewBuildingJob.ScheduleParallel(jobHandleBuildTimeSystemJob);
        jobHandleSetUpDataNewBuildingJob.Complete();
        ecb_SetUpDataNewBuildingJob.Playback(state.EntityManager);
        ecb_SetUpDataNewBuildingJob.Dispose();


    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct BuildTimeSystemJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float DeltaTime;
        //[ReadOnly] public BufferLookup<SnapPointBuffer> SnapPointsBuffersLookup;
        //[ReadOnly] public BufferLookup<SnapPointsDirectionBuffer> SnapPointsDirectionBuffersLookup;
        //[ReadOnly] public ComponentLookup<LocalToWorld> componentLocalToWorldLookup;
        //[ReadOnly] public CollisionWorld collisionWorld;
        public void Execute([ChunkIndexInQuery] int sortkey, ref BuildingGhost buildingGhost, ref IsBuilding isBuilding, in LocalTransform LocalTransform, Entity entity)
        {
            buildingGhost.timeBuild -= DeltaTime;
            if (buildingGhost.timeBuild > 0) return;
            Entity building = ecb.Instantiate(sortkey, buildingGhost.buildingEntity);
            ecb.SetComponent<LocalTransform>(sortkey, building, new LocalTransform
            {
                Position = LocalTransform.Position,
                Rotation = LocalTransform.Rotation,
                Scale = LocalTransform.Scale,
            });
            ecb.SetComponent<IsBuilding>(sortkey, entity, new IsBuilding
            {
                buildingEntity = building,
            });
            //DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers = SnapPointsDirectionBuffersLookup[entity];
            //DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBufferBuildings = SnapPointsDirectionBuffersLookup[building];
            //for (int i = 0; i < snapPointsDirectionBuffers.Length; i++)
            //{
            //    DynamicBuffer<SnapPointBuffer> snapPointBuffers = SnapPointsBuffersLookup[snapPointsDirectionBuffers[i].SnapPointsDirectionEntity];
            //    DynamicBuffer<SnapPointBuffer> snapPointBufferBuildings = ecb.SetBuffer<SnapPointBuffer>(sortkey, snapPointsDirectionBufferBuildings[i].SnapPointsDirectionEntity);
            //    for (int j = 0; j < snapPointBuffers.Length; j++)
            //    {
            //        snapPointBufferBuildings.Add(new SnapPointBuffer
            //        {
            //            snapPointPosition = componentLocalToWorldLookup[snapPointBuffers[j].snapPointEntity].Position,
            //            snapPointType = snapPointBuffers[j].snapPointType,
            //            isOccupied = false,
            //            distanceSnapPointToBuildingGhost = snapPointBuffers[j].distanceSnapPointToBuildingGhost,
            //            offset = snapPointBuffers[j].offset,
            //        });
            //        RaycastInput raycastInput = new()
            //        {
            //            Start = snapPointBuffers[j].snapPointPosition,
            //            End = snapPointBuffers[j].snapPointPosition + snapPointsDirectionBuffers[i].directionVector * 0.1f,
            //            Filter = new CollisionFilter
            //            {
            //                BelongsTo = ~0u,
            //                CollidesWith = 1u << 8,
            //                GroupIndex = 0,
            //            }
            //        };
            //        MyCollector.IgnoreEntityCollector ignoreEntityCollector = new(entity, 1.0f, Allocator.TempJob);
            //        if(collisionWorld.CastRay(raycastInput, ref ignoreEntityCollector))
            //        {
            //            if (ignoreEntityCollector.Hits.Length > 0)
            //            {
            //                ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, ignoreEntityCollector.Hits[0].Entity, true);
            //            }
            //        }

            //    }
            //}
            //ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, building, true);
            //ecb.DestroyEntity(sortkey, entity);
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
        public void Execute([ChunkIndexInQuery] int sortkey, ref BuildingGhost buildingGhost, in IsBuilding isBuilding, in LocalTransform LocalTransform, Entity entity)
        {
            if(isBuilding.buildingEntity == Entity.Null) return;
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
}
