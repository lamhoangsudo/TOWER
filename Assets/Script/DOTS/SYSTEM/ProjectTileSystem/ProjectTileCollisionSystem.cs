using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
partial struct ProjectTileCollisionSystem : ISystem
{
    private CollisionFilter collisionFilter;
    private RaycastInput raycastInput;
    private RaycastHit raycastHit;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        ProjectTileCollisionJob projectTileCollisionJob = new()
        {
            collisionWorld = collisionWorld,
            ecb = ecb.AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            targetLocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            targetTargetLookup = SystemAPI.GetComponentLookup<Target>(isReadOnly: true),
        };
        JobHandle projectTileCollisionJobHandle = projectTileCollisionJob.ScheduleParallel(state.Dependency);
        projectTileCollisionJobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct ProjectTileCollisionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> targetLocalTransformLookup;
        [ReadOnly] public ComponentLookup<Target> targetTargetLookup;
        [ReadOnly] public CollisionWorld collisionWorld;
        public EntityCommandBuffer.ParallelWriter ecb;
        public float DeltaTime;
        private CollisionFilter collisionFilter;
        private RaycastInput raycastInput;
        private RaycastHit raycastHit;
        public void Execute([ChunkIndexInQuery] int sortkey, ref ProjecTile projecTile, in LocalTransform localTransform, Entity entity)
        {
            switch (projecTile.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    if (projecTile.timeDelayRayMax == 0)
                    {
                        projecTile.timeDelayRayMax = projecTile.targetDistance / (projecTile.projecTileCurrentSpeed * 120f);
                        projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                    }
                    projecTile.timeDelayRay -= DeltaTime;
                    if (projecTile.timeDelayRay > 0) return;
                    collisionFilter = new()
                    {
                        BelongsTo = ~0u,
                        CollidesWith = ~0u,
                        GroupIndex = 0,
                    };
                    raycastInput = new()
                    {
                        Start = localTransform.Position,
                        End = localTransform.Position + projecTile.projecTileCurrentSpeed * DeltaTime * localTransform.Forward(),
                        Filter = collisionFilter,
                    };
                    if (collisionWorld.CastRay(raycastInput, out raycastHit))
                    {
                        if (targetTargetLookup.HasComponent(raycastHit.Entity))
                        {
                            Entity projectileExplosionEntity = ecb.Instantiate(sortkey, projecTile.projectileExplosion);
                            ecb.SetComponent<LocalTransform>(sortkey, projectileExplosionEntity, new LocalTransform
                            {
                                Position = raycastHit.Position,
                                Rotation = quaternion.identity,
                                Scale = 1f,
                            });
                            ecb.DestroyEntity(sortkey, entity);
                        }
                    }
                    projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                    break;
                case Enum.ProjectTileType.Missile:
                    float3 missilePosition = localTransform.Position;
                    float3 targetPosition = targetLocalTransformLookup[projecTile.homingTarget].Position;
                    float distanceTarget = math.distance(missilePosition, targetPosition);
                    if (projecTile.timeDelayRayMax == 0)
                    {
                        projecTile.timeDelayRayMax = distanceTarget / (projecTile.projecTileMaxSpeed * 120f);
                        projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                    }
                    projecTile.timeDelayRay -= DeltaTime;
                    if (projecTile.timeDelayRay > 0) return;
                    collisionFilter = new()
                    {
                        BelongsTo = ~0u,
                        CollidesWith = ~0u,
                        GroupIndex = 0,
                    };
                    raycastInput = new()
                    {
                        Start = localTransform.Position,
                        End = localTransform.Position + projecTile.projecTileCurrentSpeed * DeltaTime * localTransform.Forward(),
                        Filter = collisionFilter,
                    };
                    if (collisionWorld.CastRay(raycastInput, out raycastHit))
                    {
                        if (targetTargetLookup.HasComponent(raycastHit.Entity))
                        {
                            Entity projectileExplosionEntity = ecb.Instantiate(sortkey, projecTile.projectileExplosion);
                            ecb.SetComponent<LocalTransform>(sortkey, projectileExplosionEntity, new LocalTransform
                            {
                                Position = raycastHit.Position,
                                Rotation = quaternion.identity,
                                Scale = 1f,
                            });
                            ecb.DestroyEntity(sortkey, entity);
                        }
                    }
                    projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                    break;
            }
        }
    }
}
