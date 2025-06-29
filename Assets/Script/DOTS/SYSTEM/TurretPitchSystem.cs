using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct TurretPitchSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (RefRW<Turret> turret in SystemAPI.Query<RefRW<Turret>>())
        {
            if (turret.ValueRO.elevationPivot == Entity.Null)
                continue;

            float elevation = turret.ValueRW.currentElevation;
            float speed = turret.ValueRW.currentElevationSpeed;
            float targetElevation;

            if (turret.ValueRO.target != Entity.Null)
            {
                if (
                    SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.target) &&
                    SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.elevationPivot)
                )
                {
                    LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(turret.ValueRO.target);
                    LocalTransform pivotTransform = SystemAPI.GetComponent<LocalTransform>(turret.ValueRO.elevationPivot);

                    float3 targetPos = targetTransform.Position;
                    float3 pivotPos = pivotTransform.Position;

                    float3 toTarget = pivotPos - targetPos;

                    float distanceXZ = math.distance(targetPos, pivotPos);
                    targetElevation = math.degrees(math.atan2(toTarget.y, distanceXZ));
                }
                else
                {
                    targetElevation = elevation; // target không hợp lệ
                }
            }
            else
            {
                targetElevation = turret.ValueRO.resetOrientation ? 0f : elevation;
            }

            // delta angle (elevation)

            float deltaAngle = targetElevation - elevation;
            deltaAngle = (deltaAngle + 180f) % 360f - 180f;

            // tăng giảm tốc độ
            if (math.abs(deltaAngle) > 1f)
            {
                speed += turret.ValueRO.elevationRotationAcceleration * deltaTime;
            }
            else
            {
                speed -= turret.ValueRO.elevationRotationAcceleration * deltaTime;
            }

            speed = math.clamp(speed, 0f, turret.ValueRO.elevationRotationSpeed);
            
            // bước nâng
            float rotationStep = speed * deltaTime;

            if (math.abs(deltaAngle) < rotationStep)
            {
                elevation = targetElevation;
            }
            else
            {
                elevation += rotationStep * math.sign(deltaAngle);
            }

            // clamp pitch nếu cần
            if (turret.ValueRO.elevationLimited)
            {
                elevation = math.clamp(
                    elevation,
                    turret.ValueRO.minElevationLimit,
                    turret.ValueRO.maxElevationLimit
                );
            }

            // lưu state
            turret.ValueRW.currentElevation = elevation;
            turret.ValueRW.currentElevationSpeed = speed;
            turret.ValueRW.elevationSpeedFactor = math.abs(turret.ValueRO.currentElevationSpeed) / turret.ValueRO.elevationRotationSpeed;

            // apply transform
            if (SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.elevationPivot))
            {
                RefRW<LocalTransform> pivotTransformRW =
                    SystemAPI.GetComponentRW<LocalTransform>(turret.ValueRO.elevationPivot);

                pivotTransformRW.ValueRW = pivotTransformRW.ValueRW.WithRotation(
                    quaternion.Euler(math.radians(elevation), 0, 0)
                );
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
