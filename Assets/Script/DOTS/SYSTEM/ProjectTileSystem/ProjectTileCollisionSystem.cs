using System.Globalization;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static UnityEngine.EventSystems.EventTrigger;
partial struct ProjectTileCollisionSystem : ISystem
{
    private EntityQuery query;
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        query = SystemAPI.QueryBuilder().WithAll<ProjecTile, LocalTransform>().Build();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        ProjectTileCollisionChunk projectTileCollisionChunk = new()
        {
            collisionWorld = collisionWorld,
            ecb = ecb.AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            targetLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
            targetTargetLookup = SystemAPI.GetComponentLookup<Enemy>(isReadOnly: true),
            entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
            localTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(),
            projecTileHandle = SystemAPI.GetComponentTypeHandle<ProjecTile>(),
        };
        JobHandle projectTileCollisionChunkJobHandle = projectTileCollisionChunk.ScheduleParallel(query, state.Dependency);
        projectTileCollisionChunkJobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    //[BurstCompile]
    public partial struct ProjectTileCollisionChunk : IJobChunk
    {
        [ReadOnly] public ComponentLookup<LocalToWorld> targetLocalToWorldLookup;
        [ReadOnly] public ComponentLookup<Enemy> targetTargetLookup;
        [ReadOnly] public CollisionWorld collisionWorld;
        public EntityCommandBuffer.ParallelWriter ecb;
        public float DeltaTime;
        public ComponentTypeHandle<ProjecTile> projecTileHandle;
        public ComponentTypeHandle<LocalTransform> localTransformHandle;
        public EntityTypeHandle entityTypeHandle;
        private CollisionFilter collisionFilter;
        private RaycastInput raycastInput;
        private RaycastHit raycastHit;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<ProjecTile> projecTiles = chunk.GetNativeArray(ref projecTileHandle);
            NativeArray<LocalTransform> localTransforms = chunk.GetNativeArray(ref localTransformHandle);
            NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
            for(int i = 0; i < chunk.Count; i++)
            {
                ProjecTile projecTile = projecTiles[i];
                Entity entity = entities[i];
                LocalTransform localTransform = localTransforms[i];
                switch (projecTile.projectTileType)
                {
                    case Enum.ProjectTileType.Bullet:
                        if (projecTile.timeDelayRayMax == 0)
                        {
                            projecTile.timeDelayRayMax = projecTile.targetDistance / (projecTile.projecTileCurrentSpeed * 120f);
                            projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                        }
                        projecTile.timeDelayRay -= DeltaTime;
                        if (projecTile.timeDelayRay > 0) continue;
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
                                Entity projectileExplosionEntity = ecb.Instantiate(unfilteredChunkIndex, projecTile.projectileExplosion);
                                ecb.SetComponent<LocalTransform>(unfilteredChunkIndex, projectileExplosionEntity, new LocalTransform
                                {
                                    Position = raycastHit.Position,
                                    Rotation = quaternion.identity,
                                    Scale = 1f,
                                });
                                ecb.DestroyEntity(unfilteredChunkIndex, entity);
                            }
                        }
                        projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                        projecTiles[i] = projecTile;
                        break;
                    case Enum.ProjectTileType.Missile:
                        float3 missilePosition = localTransform.Position;
                        float3 targetPosition = targetLocalToWorldLookup[projecTile.homingTarget].Position;
                        float distanceTarget = math.distance(missilePosition, targetPosition);
                        if (projecTile.timeDelayRayMax == 0)
                        {
                            projecTile.timeDelayRayMax = distanceTarget / (projecTile.projecTileMaxSpeed * 120f);
                            projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                        }
                        projecTile.timeDelayRay -= DeltaTime;
                        if (projecTile.timeDelayRay > 0) continue;
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
                                Entity projectileExplosionEntity = ecb.Instantiate(unfilteredChunkIndex, projecTile.projectileExplosion);
                                ecb.SetComponent<LocalTransform>(unfilteredChunkIndex, projectileExplosionEntity, new LocalTransform
                                {
                                    Position = raycastHit.Position,
                                    Rotation = quaternion.identity,
                                    Scale = 1f,
                                });
                                ecb.DestroyEntity(unfilteredChunkIndex, entity);
                            }
                        }
                        projecTile.timeDelayRay = projecTile.timeDelayRayMax;
                        projecTiles[i] = projecTile;
                        break;
                }
            }
        }
    }
}
