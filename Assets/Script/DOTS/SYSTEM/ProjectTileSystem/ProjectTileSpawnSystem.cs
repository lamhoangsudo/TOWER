using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ProjectTileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        /*
        foreach ((RefRW<ProjectTileSpawnShoot> projectTileSpawnShoot, RefRO<LocalToWorld> localToWorld) in SystemAPI.Query<RefRW<ProjectTileSpawnShoot>, RefRO<LocalToWorld>>())
        {
            if (!projectTileSpawnShoot.ValueRO.isSpawner) continue;

            Entity entityProjectTileSpawn = state.EntityManager.Instantiate(projectTileSpawnShoot.ValueRO.entityProjectTilePrefab);

            RefRW<ProjecTile> projectTileSpawn = SystemAPI.GetComponentRW<ProjecTile>(entityProjectTileSpawn);
            RefRW<LocalTransform> projectTileSpawnLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(entityProjectTileSpawn);

            projectTileSpawnLocalTransform.ValueRW.Position = localToWorld.ValueRO.Position;
            projectTileSpawnLocalTransform.ValueRW.Rotation = localToWorld.ValueRO.Rotation;

            projectTileSpawn.ValueRW.projecTileLifetimeMax = projectTileSpawnShoot.ValueRO.projectileLifetimeMax;
            projectTileSpawn.ValueRW.projecTileCurrentLifetime = projectTileSpawnShoot.ValueRO.projectileLifetimeMax;
            switch (projectTileSpawn.ValueRO.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    projectTileSpawn.ValueRW.projecTileMaxSpeed = projectTileSpawnShoot.ValueRO.projectileMaxSpeed;
                    projectTileSpawn.ValueRW.projecTileCurrentSpeed = projectTileSpawnShoot.ValueRO.projectileMaxSpeed;
                    projectTileSpawn.ValueRW.targetDistance = math.distance(localToWorld.ValueRO.Position, projectTileSpawnShoot.ValueRO.targetPosition); ;
                    break;
                case Enum.ProjectTileType.Missile:
                    projectTileSpawn.ValueRW.homingTarget = projectTileSpawnShoot.ValueRO.homingTarget;
                    projectTileSpawn.ValueRW.projecTileAcceleration = projectTileSpawnShoot.ValueRO.projectileAcceleration;
                    projectTileSpawn.ValueRW.projecTileCurrentSpeed = projectTileSpawnShoot.ValueRO.projectileStartSpeed;
                    projectTileSpawn.ValueRW.projecTileMaxSpeed = projectTileSpawnShoot.ValueRO.projectileMaxSpeed;
                    projectTileSpawn.ValueRW.targetDistance = math.distance(localToWorld.ValueRO.Position, projectTileSpawnShoot.ValueRO.targetPosition); ;
                    break;
            }
            projectTileSpawnShoot.ValueRW.isSpawner = false;
        }
        */
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        ProjectTileSpawnJob projectTileSpawnJob = new()
        {
            projectTileSpawnLookUp = SystemAPI.GetComponentLookup<ProjecTile>(isReadOnly: false),
            projectTileSpawnLocalTransformLookUp = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            ecb = ecb.AsParallelWriter(),
        };
        JobHandle projectTileSpawnJobHandle = projectTileSpawnJob.ScheduleParallel(state.Dependency);
        projectTileSpawnJobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct ProjectTileSpawnJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ProjecTile> projectTileSpawnLookUp;
        [ReadOnly] public ComponentLookup<LocalTransform> projectTileSpawnLocalTransformLookUp;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute([ChunkIndexInQuery] int sortkey, ref ProjectTileSpawnShoot projectTileSpawnShoot, in LocalToWorld localToWorld)
        {
            if (!projectTileSpawnShoot.isSpawner) return;
            Entity entityProjectTileSpawn = ecb.Instantiate(sortkey, projectTileSpawnShoot.entityProjectTilePrefab);
            ProjecTile projectTileSpawnWriter = projectTileSpawnLookUp[entityProjectTileSpawn];
            LocalTransform projectTileSpawnLocalTransformWriter = projectTileSpawnLocalTransformLookUp[entityProjectTileSpawn];
            projectTileSpawnLocalTransformWriter.Position = localToWorld.Position;
            projectTileSpawnLocalTransformWriter.Rotation = localToWorld.Rotation;

            projectTileSpawnWriter.projecTileLifetimeMax = projectTileSpawnShoot.projectileLifetimeMax;
            projectTileSpawnWriter.projecTileCurrentLifetime = projectTileSpawnShoot.projectileLifetimeMax;
            switch (projectTileSpawnWriter.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    projectTileSpawnWriter.projecTileMaxSpeed = projectTileSpawnShoot.projectileMaxSpeed;
                    projectTileSpawnWriter.projecTileCurrentSpeed = projectTileSpawnShoot.projectileMaxSpeed;
                    projectTileSpawnWriter.targetDistance = math.distance(localToWorld.Position, projectTileSpawnShoot.targetPosition); ;
                    break;
                case Enum.ProjectTileType.Missile:
                    projectTileSpawnWriter.homingTarget = projectTileSpawnShoot.homingTarget;
                    projectTileSpawnWriter.projecTileAcceleration = projectTileSpawnShoot.projectileAcceleration;
                    projectTileSpawnWriter.projecTileCurrentSpeed = projectTileSpawnShoot.projectileStartSpeed;
                    projectTileSpawnWriter.projecTileMaxSpeed = projectTileSpawnShoot.projectileMaxSpeed;
                    projectTileSpawnWriter.targetDistance = math.distance(localToWorld.Position, projectTileSpawnShoot.targetPosition); ;
                    break;
            }
            projectTileSpawnShoot.isSpawner = false;
            ecb.SetComponent<ProjecTile>(sortkey, entityProjectTileSpawn, projectTileSpawnWriter);
            ecb.SetComponent<LocalTransform>(sortkey, entityProjectTileSpawn, projectTileSpawnLocalTransformWriter);
        }
    }
}
