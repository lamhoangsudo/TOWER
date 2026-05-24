using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Di chuyển projectile: Bullet bay thẳng, Missile có homing + acceleration.
/// </summary>
[BurstCompile]
partial struct ProjectileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, Projectile>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ProjectileMovementJob job = new()
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
public partial struct ProjectileMovementJob : IJobEntity
{
    public float DeltaTime;
    [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldLookup;

    public void Execute(ref LocalTransform localTransform, ref Projectile projectile)
    {
        // Lưu vị trí trước khi di chuyển — dùng cho anti-tunneling raycast
        projectile.previousPosition = localTransform.Position;

        switch (projectile.projectileType)
        {
            case Enum.ProjectileType.Bullet:
                localTransform.Position += projectile.projectileCurrentSpeed * DeltaTime * localTransform.Forward();
                break;

            case Enum.ProjectileType.Missile:
                // Acceleration
                projectile.projectileCurrentSpeed += projectile.projectileAcceleration * DeltaTime;
                projectile.projectileCurrentSpeed = math.clamp(projectile.projectileCurrentSpeed, 0f, projectile.projectileMaxSpeed);

                // Movement
                localTransform.Position += projectile.projectileCurrentSpeed * DeltaTime * localTransform.Forward();

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
