using Unity.Burst;
using Unity.Entities;
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
                    float distance = math.distancesq(localToWorld.ValueRO.Position, projectTileSpawnShoot.ValueRO.targetPosition);
                    projectTileSpawn.ValueRW.projecTileMaxSpeed = projectTileSpawnShoot.ValueRO.projectileMaxSpeed;
                    projectTileSpawn.ValueRW.projecTileCurrentSpeed = projectTileSpawnShoot.ValueRO.projectileMaxSpeed;
                    projectTileSpawn.ValueRW.targetDistance = distance;
                    break;
                case Enum.ProjectTileType.Missile:
                    projectTileSpawn.ValueRW.projecTileAcceleration = projectTileSpawnShoot.ValueRO.projectileAcceleration;
                    projectTileSpawn.ValueRW.projecTileCurrentSpeed = projectTileSpawnShoot.ValueRO.projectileStartSpeed;
                    break;
            }
            projectTileSpawnShoot.ValueRW.isSpawner = false;
        }

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
