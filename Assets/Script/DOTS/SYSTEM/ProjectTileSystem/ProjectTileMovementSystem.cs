using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Di chuyển projectile: Bullet bay thẳng, Missile có homing + acceleration.
/// </summary>
[BurstCompile]
partial struct ProjectTileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, ProjecTile>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ProjectTileMovementJob job = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
        };
        job.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct ProjectTileMovementJob : IJobEntity
{
    public float DeltaTime;
    [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldLookup;

    public void Execute(ref LocalTransform localTransform, ref ProjecTile projectile)
    {
        switch (projectile.projectTileType)
        {
            case Enum.ProjectTileType.Bullet:
                localTransform.Position += projectile.projecTileCurrentSpeed * DeltaTime * localTransform.Forward();
                break;

            case Enum.ProjectTileType.Missile:
                // Acceleration
                projectile.projecTileCurrentSpeed += projectile.projecTileAcceleration * DeltaTime;
                projectile.projecTileCurrentSpeed = math.clamp(projectile.projecTileCurrentSpeed, 0f, projectile.projecTileMaxSpeed);

                // Movement
                localTransform.Position += projectile.projecTileCurrentSpeed * DeltaTime * localTransform.Forward();

                // Homing
                if (projectile.homingTarget != Entity.Null && localToWorldLookup.HasComponent(projectile.homingTarget))
                {
                    float3 targetPos = localToWorldLookup[projectile.homingTarget].Position;
                    float3 direction = targetPos - localTransform.Position;
                    float3 forward = localTransform.Forward();

                    float dot = math.clamp(math.dot(math.normalizesafe(forward), math.normalizesafe(direction)), -1f, 1f);
                    float angle = math.acos(dot);

                    if (angle > 1e-5f)
                    {
                        float t = math.min(1f, (math.radians(projectile.homingSpeed) * DeltaTime) / angle);
                        localTransform.Rotation = math.slerp(localTransform.Rotation, quaternion.LookRotationSafe(direction, math.up()), t);
                    }
                }
                break;
        }
    }
}
