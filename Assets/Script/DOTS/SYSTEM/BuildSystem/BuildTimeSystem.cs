using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
[UpdateBefore(typeof(SimulationSystemGroup))]
public partial struct BuildTimeSystem : ISystem
{
    public EntityCommandBuffer ecb_BuildTimeSystemJob;
    public EntityCommandBuffer ecb_SetUpDataNewBuildingJob;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //ecb_BuildTimeSystemJob = new EntityCommandBuffer(Allocator.TempJob);
        ecb_BuildTimeSystemJob = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        BuildTimeSystemJob buildTimeSystemJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb_BuildTimeSystemJob.AsParallelWriter(),
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
        }
    }
}
