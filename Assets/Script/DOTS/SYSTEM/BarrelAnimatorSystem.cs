using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
/// <summary>
/// System to animate the barrel base rotation, barrel tip recoil,
/// and toggle the muzzle flash entity based on firing state.
/// </summary>
public partial struct BarrelAnimatorSystem : ISystem
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
            RefRO<BarrelAnimator> barrel in SystemAPI.Query<RefRO<BarrelAnimator>>()
        )
        {
            // rotate the barrel base entity
            if (SystemAPI.Exists(barrel.ValueRO.barrelBaseEntity))
            {
                RefRW<LocalTransform> baseTransform = SystemAPI.GetComponentRW<LocalTransform>(barrel.ValueRO.barrelBaseEntity);
                baseTransform.ValueRW.Rotation = math.mul(
                    baseTransform.ValueRO.Rotation,
                    quaternion.RotateY(math.radians(barrel.ValueRO.rotationSpeed * deltaTime))
                );
            }

            // handle recoil on the barrel tip entity
            if (SystemAPI.Exists(barrel.ValueRO.barrelTipEntity))
            {
                RefRW<LocalTransform> tipTransform = SystemAPI.GetComponentRW<LocalTransform>(barrel.ValueRO.barrelTipEntity);

                float3 currentPosition = tipTransform.ValueRO.Position;
                float3 targetPosition = barrel.ValueRO.initialPosition;

                if (barrel.ValueRO.isFiring)
                {
                    targetPosition -= tipTransform.ValueRO.Forward() * barrel.ValueRO.recoilDistance;
                }

                tipTransform.ValueRW.Position = math.lerp(
                    currentPosition,
                    targetPosition,
                    math.min(barrel.ValueRO.recoilSpeed * deltaTime, 1f)
                );
            }

            // toggle the muzzle flash entity (enable/disable)
            if (SystemAPI.Exists(barrel.ValueRO.muzzleFlashEntity))
            {
                RefRW<LocalTransform> flashTransform = SystemAPI.GetComponentRW<LocalTransform>(barrel.ValueRO.muzzleFlashEntity);

                // move muzzle flash out of view if not firing
                flashTransform.ValueRW.Position = barrel.ValueRO.isFiring
                    ? flashTransform.ValueRO.Position
                    : new float3(9999, 9999, 9999);
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
