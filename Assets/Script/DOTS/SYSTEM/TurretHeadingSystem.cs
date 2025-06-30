using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct TurretHeadingSystem : ISystem
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
            // Bỏ qua nếu không có pivot
            if (turret.ValueRO.headingPivot == Entity.Null)
                continue;

            float heading = turret.ValueRW.currentHeading;
            float speed = turret.ValueRW.currentHeadingSpeed;
            float targetHeading;

            if (turret.ValueRO.target != Entity.Null)
            {
                if (
                    SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.target) &&
                    SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.headingPivot)
                )
                {
                    LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(turret.ValueRO.target);
                    LocalTransform pivotTransform = SystemAPI.GetComponent<LocalTransform>(turret.ValueRO.headingPivot);

                    float3 targetPos = targetTransform.Position;
                    float3 pivotPos = pivotTransform.Position;

                    float3 toTarget = targetPos - pivotPos;

                    targetHeading = math.degrees(math.atan2(toTarget.x, toTarget.z));
                    if ((targetHeading < turret.ValueRO.minHeadingLimit || targetHeading > turret.ValueRO.maxHeadingLimit) && turret.ValueRO.headingLimited)
                    {
                        targetHeading = heading;
                    }
                }
                else
                {
                    targetHeading = heading; // target không hợp lệ
                }
            }
            else
            {
                targetHeading = turret.ValueRO.resetOrientation ? 0f : heading;
            }

            // normalize delta angle -180..180
            float deltaAngle = targetHeading - heading;
            deltaAngle = (deltaAngle + 180f) % 360f - 180f;
            if (math.abs(deltaAngle) < turret.ValueRO.targetAquiredAngle)
            {
                turret.ValueRW.isHeadingRotationTarget = true;
            }
            else
            {
                turret.ValueRW.isHeadingRotationTarget = false;
            }
            // tăng giảm tốc độ
            if (math.abs(deltaAngle) > 1f)
            {
                speed += turret.ValueRO.headingRotationAcceleration * deltaTime;
            }
            else
            {
                speed -= turret.ValueRO.headingRotationAcceleration * deltaTime;
            }

            speed = math.clamp(speed, 0f, turret.ValueRO.headingRotationSpeed);

            // bước xoay
            float rotationStep = speed * deltaTime;

            if (math.abs(deltaAngle) < rotationStep)
            {
                heading = targetHeading;
            }
            else
            {
                heading += rotationStep * math.sign(deltaAngle);
            }

            // clamp heading nếu cần
            if (turret.ValueRO.headingLimited)
            {
                heading = math.clamp(
                    heading,
                    turret.ValueRO.minHeadingLimit,
                    turret.ValueRO.maxHeadingLimit
                );
                if (heading <= turret.ValueRO.minHeadingLimit || heading >= turret.ValueRO.maxHeadingLimit)
                {
                    speed = 0f; // dừng xoay nếu chạm giới hạn
                }
            }

            // lưu state
            turret.ValueRW.currentHeading = heading;
            turret.ValueRW.currentHeadingSpeed = speed;
            turret.ValueRW.headingSpeedFactor = math.abs(speed) / turret.ValueRO.headingRotationSpeed;
            if (turret.ValueRO.headingSpeedFactor > 0.05f)
            {
                turret.ValueRW.IsHeadingRotationSFX = true;
            }
            else
            {
                turret.ValueRW.IsHeadingRotationSFX = false;
            }
            // apply transform
            if (SystemAPI.HasComponent<LocalTransform>(turret.ValueRO.headingPivot))
            {
                RefRW<LocalTransform> pivotTransformRW =
                    SystemAPI.GetComponentRW<LocalTransform>(turret.ValueRO.headingPivot);

                pivotTransformRW.ValueRW = pivotTransformRW.ValueRW.WithRotation(
                    quaternion.Euler(0, math.radians(heading), 0)
                );
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
