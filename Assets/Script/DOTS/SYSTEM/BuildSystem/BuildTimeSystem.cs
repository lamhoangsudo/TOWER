using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial struct BuildTimeSystem : ISystem
{
    public EntityCommandBuffer ecb;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        BuildTimeSystemJob buildTimeSystemJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
            componentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            SnapPointsBuffersLookup = SystemAPI.GetBufferLookup<SnapPointBuffer>(isReadOnly: true),
            SnapPointsDirectionBuffersLookup = SystemAPI.GetBufferLookup<SnapPointsDirectionBuffer>(isReadOnly: true),
        };
        buildTimeSystemJob.ScheduleParallel();
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
        [ReadOnly] public BufferLookup<SnapPointBuffer> SnapPointsBuffersLookup;
        [ReadOnly] public BufferLookup<SnapPointsDirectionBuffer> SnapPointsDirectionBuffersLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> componentLocalToWorldLookup;
        [ReadOnly] public CollisionWorld collisionWorld;
        public void Execute([ChunkIndexInQuery] int sortkey, ref BuildingGhost buildingGhost, in IsBuilding isBuilding, in LocalTransform LocalTransform, Entity entity)
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
            DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers = SnapPointsDirectionBuffersLookup[entity];
            for (int i = 0; i < snapPointsDirectionBuffers.Length; i++)
            {
                DynamicBuffer<SnapPointBuffer> snapPointBuffers = SnapPointsBuffersLookup[snapPointsDirectionBuffers[i].SnapPointsDirectionEntity];
                for(int j = 0; j < snapPointBuffers.Length; j++)
                {
                    LocalToWorld localToWorld = componentLocalToWorldLookup[snapPointBuffers[j].snapPointEntity];
                    RaycastInput raycastInput = new()
                    {
                        Start = localToWorld.Position,
                        End = localToWorld.Position + localToWorld.Forward * 0.1f,
                        Filter = new CollisionFilter
                        {
                            BelongsTo = ~0u,
                            CollidesWith = 1u << 8,
                            GroupIndex = 0,
                        }
                    };
                    MyCollector.IgnoreEntityCollector ignoreEntityCollector = new(entity, 1.0f, Allocator.Temp);
                    if(collisionWorld.CastRay(raycastInput, ref ignoreEntityCollector))
                    {
                        if (ignoreEntityCollector.Hits.Length > 0)
                        {
                            ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, ignoreEntityCollector.Hits[0].Entity, true);
                        }
                    }
                }
            }
            ecb.SetComponentEnabled<IsCheckSnapPoint>(sortkey, building, true);
            ecb.DestroyEntity(sortkey, entity);
        }
    }
}
