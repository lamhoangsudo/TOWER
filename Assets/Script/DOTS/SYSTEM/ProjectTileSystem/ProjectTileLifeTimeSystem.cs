using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
partial struct ProjectTileLifeTimeSystem : ISystem
{
    private EntityQuery queryProjectTileLifeTimeJobChunk;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        queryProjectTileLifeTimeJobChunk = SystemAPI.QueryBuilder().WithAll<ProjecTile>().Build();
        state.RequireForUpdate(queryProjectTileLifeTimeJobChunk);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        ProjectTileLifeTimeJobChunk projectTileLifeTimeJobChunk = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
            projecTileHandle = SystemAPI.GetComponentTypeHandle<ProjecTile>(),
            entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
        };
        JobHandle jobHandle = projectTileLifeTimeJobChunk.ScheduleParallel(queryProjectTileLifeTimeJobChunk, state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    [BurstCompile]
    public partial struct ProjectTileLifeTimeJobChunk : IJobChunk
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;
        public ComponentTypeHandle<ProjecTile> projecTileHandle;
        public EntityTypeHandle entityTypeHandle;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<ProjecTile> projecTiles = chunk.GetNativeArray(ref projecTileHandle);
            NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                ProjecTile projecTile = projecTiles[i];
                projecTile.projecTileCurrentLifetime -= DeltaTime;
                projecTiles[i] = projecTile;
                if (projecTiles[i].projecTileCurrentLifetime > 0f) continue;
                ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
            }
        }
    }
}
