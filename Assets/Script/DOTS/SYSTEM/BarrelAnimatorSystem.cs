using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BarrelAnimatorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float time = (float)SystemAPI.Time.ElapsedTime;

        foreach ((RefRW<BarrelAnimator> animator, Entity entity) in SystemAPI.Query<RefRW<BarrelAnimator>>().WithEntityAccess())
        {
            if (!animator.ValueRO.animationPlaying)
                continue;

            float elapsed = time - animator.ValueRO.lastFireTime;
            float progress = math.clamp(elapsed / animator.ValueRO.animationDuration, 0f, 1f);

            // Lấy blob asset
            ref BarrelAnimatorCurveBlob blob = ref animator.ValueRO.curveBlob.Value;
            int sampleCount = blob.sampleCount;
            float sampleT = progress * (sampleCount - 1);
            int idx0 = (int)math.floor(sampleT);
            int idx1 = math.min(idx0 + 1, sampleCount - 1);
            float frac = sampleT - idx0;

            // Nội suy giá trị slide và rotation
            float slideValue = math.lerp(blob.slideCurve[idx0], blob.slideCurve[idx1], frac);
            float rotationValue = math.lerp(blob.rotationCurve[idx0], blob.rotationCurve[idx1], frac);

            // Cập nhật barrel base (slide)
            if (animator.ValueRO.barrelBaseEntity != Entity.Null &&
                SystemAPI.HasComponent<LocalTransform>(animator.ValueRO.barrelBaseEntity))
            {
                RefRW<LocalTransform> baseTransform = SystemAPI.GetComponentRW<LocalTransform>(animator.ValueRO.barrelBaseEntity);
                float3 basePos = new float3(0f, 0f, -slideValue * animator.ValueRO.baseSlideDistance);
                baseTransform.ValueRW.Position = basePos;
            }

            // Cập nhật barrel tip (slide + rotation)
            if (animator.ValueRO.barrelTipEntity != Entity.Null &&
                SystemAPI.HasComponent<LocalTransform>(animator.ValueRO.barrelTipEntity))
            {
                RefRW<LocalTransform> tipTransform = SystemAPI.GetComponentRW<LocalTransform>(animator.ValueRO.barrelTipEntity);
                float tipY = animator.ValueRO.tipInitialPosition.y + slideValue * animator.ValueRO.tipSlideAmountDistance;
                float tipRotY = animator.ValueRO.tipInitialRotation.y;
                if (animator.ValueRO.tipRotateDegrees != 0f)
                {
                    tipRotY = math.lerp(animator.ValueRO.tipRotationAtFire,
                        animator.ValueRO.tipRotationAtFire + animator.ValueRO.tipRotateDegrees,
                        rotationValue);
                }
                tipTransform.ValueRW.Position = new float3(
                    animator.ValueRO.tipInitialPosition.x,
                    tipY,
                    animator.ValueRO.tipInitialPosition.z
                );
                tipTransform.ValueRW.Rotation = quaternion.Euler(
                    math.radians(animator.ValueRO.tipInitialRotation.x),
                    math.radians(tipRotY),
                    math.radians(animator.ValueRO.tipInitialRotation.z)
                );
            }

            // TODO: Cập nhật muzzle flash nếu cần

            // Tắt animation khi xong
            if (progress >= 1f)
            {
                animator.ValueRW.animationPlaying = false;
            }
        }
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
