using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
/// <summary>
/// System to rotate turret heading (yaw) and elevation (pitch)
/// based on the tracked target.
/// </summary>
partial struct TurretControllerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (
            RefRO<TurretController> turret in SystemAPI.Query<RefRO<TurretController>>()
        )
        {
            if (turret.ValueRO.targetEntity == Entity.Null || !SystemAPI.Exists(turret.ValueRO.targetEntity))
            {
                continue;
            }

            // get target world position
            LocalTransform targetTransform = SystemAPI.GetComponentRO<LocalTransform>(turret.ValueRO.targetEntity).ValueRO;
            float3 targetPosition = targetTransform.Position;

            // rotate heading (yaw)
            if (turret.ValueRO.turretHeadingEntity != Entity.Null)
            {
                RefRW<LocalTransform> headingTransform = SystemAPI.GetComponentRW<LocalTransform>(turret.ValueRO.turretHeadingEntity);

                float3 headingForward = headingTransform.ValueRO.Forward();
                float3 directionToTarget = targetPosition - headingTransform.ValueRO.Position;

                // flatten to XZ plane
                directionToTarget.y = 0;

                if (math.lengthsq(directionToTarget) > 0.0001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(directionToTarget, math.up());
                    headingTransform.ValueRW.Rotation = math.slerp(
                        headingTransform.ValueRO.Rotation,
                        targetRotation,
                        math.clamp(turret.ValueRO.rotationSpeed * deltaTime, 0f, 1f)
                    );
                }
            }

            // rotate elevation (pitch)
            if (turret.ValueRO.turretElevationEntity != Entity.Null)
            {
                RefRW<LocalTransform> elevationTransform = SystemAPI.GetComponentRW<LocalTransform>(turret.ValueRO.turretElevationEntity);

                float3 elevationForward = elevationTransform.ValueRO.Forward();
                float3 directionToTarget = targetPosition - elevationTransform.ValueRO.Position;

                if (math.lengthsq(directionToTarget) > 0.0001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(directionToTarget, math.up());
                    elevationTransform.ValueRW.Rotation = math.slerp(
                        elevationTransform.ValueRO.Rotation,
                        targetRotation,
                        math.clamp(turret.ValueRO.rotationSpeed * deltaTime, 0f, 1f)
                    );
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
