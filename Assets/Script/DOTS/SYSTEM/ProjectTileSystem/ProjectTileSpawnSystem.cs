using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectDawn;
using ProjectDawn.Geometry2D;
partial struct ProjectTileSpawnSystem : ISystem
{
    private EntityQuery query;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        query = SystemAPI.QueryBuilder().WithAll<ProjectTileSpawnShoot, LocalToWorld>().Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        ProjectTileSpawnChuck projectTileSpawnChuck = new()
        {
            ecb = ecb.AsParallelWriter(),
            localToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
            projectTileSpawnShootHandle = SystemAPI.GetComponentTypeHandle<ProjectTileSpawnShoot>(),
        };
        JobHandle jobHandle = projectTileSpawnChuck.ScheduleParallel(query, state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    [BurstCompile]
    public partial struct ProjectTileSpawnChuck : IJobChunk
    {
        public ComponentTypeHandle<ProjectTileSpawnShoot> projectTileSpawnShootHandle;
        public ComponentTypeHandle<LocalToWorld> localToWorldHandle;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<ProjectTileSpawnShoot> projectTileSpawnShoots = chunk.GetNativeArray(ref projectTileSpawnShootHandle);
            NativeArray<LocalToWorld> localToWorlds = chunk.GetNativeArray(ref localToWorldHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                ProjectTileSpawnShoot projectTileSpawnShoot = projectTileSpawnShoots[i];
                LocalToWorld localToWorld = localToWorlds[i];
                if (!projectTileSpawnShoot.isSpawner) continue;
                ProjecTile projectTileSpawnWriter = new();
                LocalTransform projectTileSpawnLocalTransformWriter = new()
                {
                    Position = localToWorld.Position,
                    Rotation = localToWorld.Rotation,
                    Scale = 1f,
                };
                projectTileSpawnWriter.projecTileLifetimeMax = projectTileSpawnShoot.projectileLifetimeMax;
                projectTileSpawnWriter.projecTileCurrentLifetime = projectTileSpawnShoot.projectileLifetimeMax;
                switch (projectTileSpawnShoot.firingPattern)
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
                projectTileSpawnShoots[i] = projectTileSpawnShoot;
                Entity entityProjectTileSpawn = ecb.Instantiate(unfilteredChunkIndex, projectTileSpawnShoot.entityProjectTilePrefab);
                ecb.SetComponent<ProjecTile>(unfilteredChunkIndex, entityProjectTileSpawn, projectTileSpawnWriter);
                ecb.SetComponent<LocalTransform>(unfilteredChunkIndex, entityProjectTileSpawn, projectTileSpawnLocalTransformWriter);
            }
        }
    }
}
