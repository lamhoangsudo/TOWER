using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(WeaponSystem))]
partial struct BarrelAnimatorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<BarrelAnimator> animator, Entity entity) in SystemAPI.Query<RefRW<BarrelAnimator>>().WithEntityAccess())
        {
            if (!animator.ValueRO.animationPlaying) continue;

            float elapsed = (float)SystemAPI.Time.ElapsedTime - animator.ValueRO.lastFireTime;
            float progress = math.clamp(elapsed / animator.ValueRO.animationDuration, 0f, 1f);
            ref BarrelAnimatorCurveBlob blob = ref animator.ValueRO.curveBlob.Value;
            int sampleCount = blob.sampleCount;
            float sampleT = progress * (sampleCount - 1);
            int idx0 = (int)math.floor(sampleT);
            int idx1 = math.min(idx0 + 1, sampleCount - 1);
            float frac = sampleT - idx0;

            float slideValue = math.lerp(blob.slideCurve[idx0], blob.slideCurve[idx1], frac);
            float rotationValue = math.lerp(blob.rotationCurve[idx0], blob.rotationCurve[idx1], frac);

            if (animator.ValueRO.barrelBaseEntity != Entity.Null && SystemAPI.HasComponent<LocalTransform>(animator.ValueRO.barrelBaseEntity))
            {
                RefRW<LocalTransform> baseTransform = SystemAPI.GetComponentRW<LocalTransform>(animator.ValueRO.barrelBaseEntity);
                float3 basePos = new(0f, 0f, -slideValue * animator.ValueRO.baseSlideDistance);
                baseTransform.ValueRW.Position = basePos;
            }

            DynamicBuffer<BarrelTipEntityBuffer> tipBuffers = SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity);
            DynamicBuffer<PointShotEntityBuffer> pointShotBuffers = SystemAPI.GetBuffer<PointShotEntityBuffer>(entity);
            BarrelTipEntityBuffer tip = tipBuffers[animator.ValueRO.barrelTipIndex];
            PointShotEntityBuffer pointShotEntityBuffer = pointShotBuffers[animator.ValueRO.pointShootIndex];

            RefRW<LocalTransform> tipTransform = SystemAPI.GetComponentRW<LocalTransform>(tip.barrelTipEntity);

            if (tip.tipInitialPosition.Equals(float3.zero) && tip.tipInitialRotation.Equals(float3.zero))
            {
                tip.tipInitialPosition = tipTransform.ValueRO.Position;
                tip.tipInitialRotation = math.Euler(tipTransform.ValueRO.Rotation);
                tipBuffers[animator.ValueRO.barrelTipIndex] = tip; // Update the buffer element
            }

            float tipY = tip.tipInitialPosition.y + slideValue * animator.ValueRO.tipSlideAmountDistance;

            tipTransform.ValueRW.Position = new float3(
                tip.tipInitialPosition.x,
                tipY,
                tip.tipInitialPosition.z
            );

            if (animator.ValueRO.tipRotateDegrees != 0f)
            {
                float tipRotY = tip.tipInitialRotation.y;
                tipRotY = math.lerp(animator.ValueRO.tipRotationAtFire,
                    animator.ValueRO.tipRotationAtFire + animator.ValueRO.tipRotateDegrees,
                    rotationValue);
                tipTransform.ValueRW.Rotation = quaternion.Euler(
                    math.radians(tip.tipInitialRotation.x),
                    math.radians(tipRotY),
                    math.radians(tip.tipInitialRotation.z)
                );
            }



            if (!animator.ValueRO.flashSpawned)
            {
                Entity pointShoot = pointShotEntityBuffer.pointShoot;
                LocalToWorld spawnLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(pointShoot);
                Entity entityEffect = state.EntityManager.Instantiate(animator.ValueRO.muzzleFlashEntity);

                RefRW<EffectWeaponShoot> effect = SystemAPI.GetComponentRW<EffectWeaponShoot>(entityEffect);
                Unity.Mathematics.Random random = animator.ValueRO.random;
                float startScale = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.scaleVariance / 2f;
                float endScale = startScale * random.NextFloat(0.6f, 0.8f);
                float startLength = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.lengthVariance / 2f;
                float endLength = startLength * random.NextFloat(1.75f, 3f);
                float randomZ = random.NextFloat(-180f, 180f);
                float pitch = math.clamp(animator.ValueRO.sfxPitch + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
                float volume = math.clamp(animator.ValueRO.sfxVolume + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);

                effect.ValueRW.startScale = startScale;
                effect.ValueRW.endScale = endScale;
                effect.ValueRW.startLength = startLength;
                effect.ValueRW.endLength = endLength;
                effect.ValueRW.sfxPitch = pitch;
                effect.ValueRW.sfxVolume = volume;

                RefRW<LocalTransform> localTranform = SystemAPI.GetComponentRW<LocalTransform>(entityEffect);
                localTranform.ValueRW.Position = spawnLocalToWorld.Position;
                quaternion randomRot = quaternion.EulerXYZ(0f, 0f, math.radians(randomZ));
                localTranform.ValueRW.Rotation = math.mul(spawnLocalToWorld.Rotation, randomRot);
                RefRW<PostTransformMatrix> visualEffectPostTransformMatrix = SystemAPI.GetComponentRW<PostTransformMatrix>(effect.ValueRO.muzzleFlashEffect);
                visualEffectPostTransformMatrix.ValueRW.Value = float4x4.Scale(startScale, startScale, startLength);
                animator.ValueRW.random = random;
                RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot = SystemAPI.GetComponentRW<SoundWeaponEffectShoot>(pointShoot);
                soundWeaponEffectShoot.ValueRW.pitch = animator.ValueRO.sfxPitch;
                soundWeaponEffectShoot.ValueRW.volume = animator.ValueRO.sfxVolume;
                soundWeaponEffectShoot.ValueRW.isPlayOneShot = true;
                animator.ValueRW.flashSpawned = true;
            }
            if (progress >= 1f)
            {
                animator.ValueRW.animationPlaying = false;
                animator.ValueRW.flashSpawned = false;
            }
        }
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
