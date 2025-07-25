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
            ProjecTile projectTileSpawnWriter = new();
            LocalTransform projectTileSpawnLocalTransformWriter = new()
            {
                Position = localToWorld.Position,
                Rotation = localToWorld.Rotation,
                Scale = 1f,
            };
            projectTileSpawnWriter.projecTileLifetimeMax = projectTileSpawnShoot.projectileLifetimeMax;
            projectTileSpawnWriter.projecTileCurrentLifetime = projectTileSpawnShoot.projectileLifetimeMax;
            switch(projectTileSpawnShoot.firingPattern)
            {
                case Enum.WeaponFiringPattern.MissileLauncher:
                    projectTileSpawnWriter.projectTileType = Enum.ProjectTileType.Missile;
                    break;
                default: 
                    projectTileSpawnWriter.projectTileType = Enum.ProjectTileType.Bullet;
                    break;
            }
            switch (projectTileSpawnWriter.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    projectTileSpawnWriter.projecTileMaxSpeed = projectTileSpawnShoot.projectileMaxSpeed;
                    projectTileSpawnWriter.projecTileCurrentSpeed = projectTileSpawnShoot.projectileMaxSpeed;
                    projectTileSpawnWriter.targetDistance = math.distance(localToWorld.Position, projectTileSpawnShoot.targetPosition);
                    projectTileSpawnWriter.projectileExplosion = projectTileSpawnShoot.entityProjectTileExplosion;
                    break;
                case Enum.ProjectTileType.Missile:
                    projectTileSpawnWriter.homingTarget = projectTileSpawnShoot.homingTarget;
                    projectTileSpawnWriter.homingSpeed = 10f;
                    projectTileSpawnWriter.projecTileAcceleration = projectTileSpawnShoot.projectileAcceleration;
                    projectTileSpawnWriter.projecTileCurrentSpeed = projectTileSpawnShoot.projectileStartSpeed;
                    projectTileSpawnWriter.projecTileMaxSpeed = projectTileSpawnShoot.projectileMaxSpeed;
                    projectTileSpawnWriter.targetDistance = math.distance(localToWorld.Position, projectTileSpawnShoot.targetPosition);
                    projectTileSpawnWriter.projectileExplosion = projectTileSpawnShoot.entityProjectTileExplosion;
                    break;
            }
            projectTileSpawnShoot.isSpawner = false;
            Entity entityProjectTileSpawn = ecb.Instantiate(sortkey, projectTileSpawnShoot.entityProjectTilePrefab);
            ecb.SetComponent<ProjecTile>(sortkey, entityProjectTileSpawn, projectTileSpawnWriter);
            ecb.SetComponent<LocalTransform>(sortkey, entityProjectTileSpawn, projectTileSpawnLocalTransformWriter);
        }
    }
}
