using Unity.Burst;
using Unity.Collections;
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
        ProjectTileMovementJob projectTileMovementJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            localTransformTargetLookUp = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
        };
        projectTileMovementJob.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct ProjectTileMovementJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> localTransformTargetLookUp;
        public void Execute(ref LocalTransform localTransform, ref ProjecTile projecTile)
        {
            switch (projecTile.projectTileType)
            {
                case Enum.ProjectTileType.Bullet:
                    localTransform.Position += projecTile.projecTileCurrentSpeed * DeltaTime * localTransform.Forward();
                    break;
                case Enum.ProjectTileType.Missile:
                    projecTile.projecTileCurrentSpeed = projecTile.projecTileCurrentSpeed + projecTile.projecTileAcceleration * DeltaTime;
                    projecTile.projecTileCurrentSpeed = math.clamp(projecTile.projecTileCurrentSpeed, 0f, projecTile.projecTileMaxSpeed);
                    localTransform.Position += projecTile.projecTileCurrentSpeed *  DeltaTime * localTransform.Forward();
                    if (projecTile.homingTarget != Entity.Null)
                    {
                        LocalTransform localTransformTarget = localTransformTargetLookUp[projecTile.homingTarget];
                        projecTile.direction = localTransformTarget.Position - localTransform.Position;
                        projecTile.dot = math.clamp(math.dot(math.normalizesafe(localTransform.Forward()), math.normalizesafe(projecTile.direction)), -1f, 1f);
                        projecTile.angle = math.acos(projecTile.dot);
                        if (projecTile.angle < 1e-5f) return;
                        projecTile.timeRotation = math.min(1f, (math.radians(projecTile.homingSpeed) * DeltaTime) / projecTile.angle);
                        localTransform.Rotation = math.slerp(localTransform.Rotation, quaternion.LookRotationSafe(projecTile.direction, math.up()), projecTile.timeRotation);
                    }
                    break;
            }
        }
    }
}
