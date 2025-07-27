using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
partial struct ProjectTileMovementSystem : ISystem
{
    private EntityQuery queryProjectTileMovementJobChunk;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        queryProjectTileMovementJobChunk = SystemAPI.QueryBuilder().WithAll<LocalTransform, ProjecTile>().Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ProjectTileMovementJobChunk projectTileMovementJobChunk = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            localTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(isReadOnly: false),
            projecTileHandle = SystemAPI.GetComponentTypeHandle<ProjecTile>(isReadOnly: false),
            localToWorldLookUp = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
        };
        JobHandle projectTileMovementJobHandle = projectTileMovementJobChunk.Schedule(queryProjectTileMovementJobChunk, state.Dependency);
        state.Dependency = projectTileMovementJobHandle;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    [BurstCompile]
    public partial struct ProjectTileMovementJobChunk : IJobChunk
    {
        public float DeltaTime;
        public ComponentTypeHandle<LocalTransform> localTransformHandle;
        public ComponentTypeHandle<ProjecTile> projecTileHandle;
        [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldLookUp;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<LocalTransform> localTransforms = chunk.GetNativeArray(ref localTransformHandle);
            NativeArray<ProjecTile> projecTiles = chunk.GetNativeArray(ref projecTileHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                LocalTransform localTransformWritter = localTransforms[i];
                ProjecTile projecTileWritter = projecTiles[i];
                switch (projecTiles[i].projectTileType)
                {
                    case Enum.ProjectTileType.Bullet:
                        localTransformWritter.Position += projecTileWritter.projecTileCurrentSpeed * DeltaTime * localTransformWritter.Forward();
                        localTransforms[i] = localTransformWritter;
                        break;
                    case Enum.ProjectTileType.Missile:
                        projecTileWritter.projecTileCurrentSpeed = projecTileWritter.projecTileCurrentSpeed + projecTileWritter.projecTileAcceleration * DeltaTime;
                        projecTileWritter.projecTileCurrentSpeed = math.clamp(projecTileWritter.projecTileCurrentSpeed, 0f, projecTileWritter.projecTileMaxSpeed);
                        localTransformWritter.Position += projecTileWritter.projecTileCurrentSpeed * DeltaTime * localTransformWritter.Forward();
                        if (projecTileWritter.homingTarget != Entity.Null)
                        {

                            projecTileWritter.direction = localToWorldLookUp[projecTileWritter.homingTarget].Position - localTransformWritter.Position;
                            projecTileWritter.dot = math.clamp(math.dot(math.normalizesafe(localTransformWritter.Forward()), math.normalizesafe(projecTileWritter.direction)), -1f, 1f);
                            projecTileWritter.angle = math.acos(projecTileWritter.dot);
                            if (projecTileWritter.angle > 1e-5f)
                            {
                                projecTileWritter.timeRotation = math.min(1f, (math.radians(projecTileWritter.homingSpeed) * DeltaTime) / projecTileWritter.angle);
                                localTransformWritter.Rotation = math.slerp(localTransformWritter.Rotation, quaternion.LookRotationSafe(projecTileWritter.direction, math.up()), projecTileWritter.timeRotation);
                            }
                        }
                        projecTiles[i] = projecTileWritter;
                        localTransforms[i] = localTransformWritter;
                        break;
                }
            }
        }
    }
}
