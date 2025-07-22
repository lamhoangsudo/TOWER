using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
partial struct ProjectTileMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<LocalTransform> localTransform, RefRW<ProjecTile> projecTile) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<ProjecTile>>())
        {
            switch (projecTile.ValueRO.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    localTransform.ValueRW.Position += projecTile.ValueRO.projecTileCurrentSpeed * SystemAPI.Time.DeltaTime * localTransform.ValueRO.Forward();
                    break;
                case Enum.ProjectTileType.Missile:
                    projecTile.ValueRW.projecTileCurrentSpeed = projecTile.ValueRO.projecTileCurrentSpeed + projecTile.ValueRO.projecTileAcceleration * SystemAPI.Time.DeltaTime;
                    projecTile.ValueRW.projecTileCurrentSpeed = math.clamp(projecTile.ValueRO.projecTileCurrentSpeed, 0f, projecTile.ValueRO.projecTileMaxSpeed);
                    localTransform.ValueRW.Position += projecTile.ValueRO.projecTileCurrentSpeed * SystemAPI.Time.DeltaTime * localTransform.ValueRO.Forward();
                    if (projecTile.ValueRO.homingTarget != Entity.Null)
                    {
                        LocalTransform localTransformTarget = SystemAPI.GetComponent<LocalTransform>(projecTile.ValueRO.homingTarget);
                        projecTile.ValueRW.direction = localTransformTarget.Position - localTransform.ValueRO.Position;
                        projecTile.ValueRW.dot = math.clamp(math.dot(math.normalizesafe(localTransform.ValueRO.Forward()), math.normalizesafe(projecTile.ValueRO.direction)), -1f, 1f);
                        projecTile.ValueRW.angle = math.acos(projecTile.ValueRO.dot);
                        UnityEngine.Debug.DrawLine(localTransform.ValueRO.Position, localTransformTarget.Position);
                        if (projecTile.ValueRO.angle < 1e-5f) continue;
                        projecTile.ValueRW.timeRotation = math.min(1f, (math.radians(projecTile.ValueRO.homingSpeed) * SystemAPI.Time.DeltaTime) / projecTile.ValueRO.angle);
                        float3 newDirection = math.normalizesafe(math.lerp(localTransform.ValueRO.Forward(), projecTile.ValueRO.direction, projecTile.ValueRO.timeRotation));
                        localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, quaternion.LookRotationSafe(projecTile.ValueRO.direction, math.up()), projecTile.ValueRO.timeRotation);
                        UnityEngine.Debug.DrawLine(localTransform.ValueRO.Position, localTransform.ValueRO.Position + localTransform.ValueRO.Forward() * 5f);
                    }
                    break;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
