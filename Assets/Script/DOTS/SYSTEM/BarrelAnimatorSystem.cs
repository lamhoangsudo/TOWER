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
        foreach ((RefRW<BarrelAnimator> barrelAnimator, RefRO<Weapon> weapon, DynamicBuffer<BarrelTipEntityBuffer> tipBuffers, DynamicBuffer<PointShotEntityBuffer> pointShotBuffers, RefRO<WeaponFireTime> weaponFireTime)
            in
            SystemAPI.Query<RefRW<BarrelAnimator>, RefRO<Weapon>, DynamicBuffer<BarrelTipEntityBuffer>, DynamicBuffer<PointShotEntityBuffer>, RefRO<WeaponFireTime>>())
        {
            if (!barrelAnimator.ValueRO.animationPlaying) continue;

            float elapsed = (float)SystemAPI.Time.ElapsedTime - barrelAnimator.ValueRO.lastFireTime;
            float progress = math.clamp(elapsed / barrelAnimator.ValueRO.animationDuration, 0f, 1f);
            ref BarrelAnimatorCurveBlob blob = ref barrelAnimator.ValueRO.curveBlob.Value;
            int sampleCount = blob.sampleCount;
            float sampleT = progress * (sampleCount - 1);
            int idx0 = (int)math.floor(sampleT);
            int idx1 = math.min(idx0 + 1, sampleCount - 1);
            float frac = sampleT - idx0;

            float slideValue = math.lerp(blob.slideCurve[idx0], blob.slideCurve[idx1], frac);
            float rotationValue = math.lerp(blob.rotationCurve[idx0], blob.rotationCurve[idx1], frac);

            if (barrelAnimator.ValueRO.barrelBaseEntity != Entity.Null && SystemAPI.HasComponent<LocalTransform>(barrelAnimator.ValueRO.barrelBaseEntity))
            {
                RefRW<LocalTransform> baseTransform = SystemAPI.GetComponentRW<LocalTransform>(barrelAnimator.ValueRO.barrelBaseEntity);
                float3 basePos = new(0f, 0f, -slideValue * barrelAnimator.ValueRO.baseSlideDistance);
                baseTransform.ValueRW.Position = basePos;
            }

            if (weapon.ValueRO.firingPattern == Enum.WeaponFiringPattern.MissileLauncher || weapon.ValueRO.firingPattern == Enum.WeaponFiringPattern.Individual)
            {
                BarrelTipEntityBuffer tip = tipBuffers[weaponFireTime.ValueRO.barrelTipIndex];
                PointShotEntityBuffer pointShotEntityBuffer = pointShotBuffers[weaponFireTime.ValueRO.pointShootIndex];

                RefRW<LocalTransform> tipTransform = SystemAPI.GetComponentRW<LocalTransform>(tip.barrelTipEntity);

                if (tip.tipInitialPosition.Equals(float3.zero) && tip.tipInitialRotation.Equals(float3.zero))
                {
                    tip.tipInitialPosition = tipTransform.ValueRO.Position;
                    tip.tipInitialRotation = math.Euler(tipTransform.ValueRO.Rotation);
                    tipBuffers.ElementAt(weaponFireTime.ValueRO.barrelTipIndex) = tip;
                }
                
                float tipY = tip.tipInitialPosition.y + slideValue * barrelAnimator.ValueRO.tipSlideAmountDistance;

                tipTransform.ValueRW.Position = new float3(
                    tip.tipInitialPosition.x,
                    tipY,
                    tip.tipInitialPosition.z
                );

                if (barrelAnimator.ValueRO.tipRotateDegrees != 0f)
                {
                    float tipRotY = tip.tipInitialRotation.y;
                    tipRotY = math.lerp(barrelAnimator.ValueRO.tipRotationAtFire,
                        barrelAnimator.ValueRO.tipRotationAtFire + barrelAnimator.ValueRO.tipRotateDegrees,
                        rotationValue);
                    tipTransform.ValueRW.Rotation = quaternion.Euler(
                        math.radians(tip.tipInitialRotation.x),
                        math.radians(tipRotY),
                        math.radians(tip.tipInitialRotation.z)
                    );
                }

                if (!barrelAnimator.ValueRO.flashSpawned)
                {
                    Entity pointShoot = pointShotEntityBuffer.pointShoot;
                    LocalTransform spawnLocalTransform = SystemAPI.GetComponent<LocalTransform>(pointShoot);
                    Unity.Mathematics.Random random = barrelAnimator.ValueRO.random;
                    Entity entityEffect = barrelAnimator.ValueRO.muzzleFlashEntity;
                    RefRW<LocalToWorld> localToWorld = SystemAPI.GetComponentRW<LocalToWorld>(entityEffect);
                    RefRW<EffectWeaponShoot> effect = SystemAPI.GetComponentRW<EffectWeaponShoot>(entityEffect);
                    float startScale = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.scaleVariance / 2f;
                    float endScale = startScale * random.NextFloat(0.6f, 0.8f);
                    float startLength = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.lengthVariance / 2f;
                    float endLength = startLength * random.NextFloat(1.75f, 3f);
                    float randomZ = random.NextFloat(-180f, 180f);
                    float pitch = math.clamp(barrelAnimator.ValueRO.sfxPitch + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
                    float volume = math.clamp(barrelAnimator.ValueRO.sfxVolume + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
                    effect.ValueRW.startScale = startScale;
                    effect.ValueRW.endScale = endScale;
                    effect.ValueRW.startLength = startLength;
                    effect.ValueRW.endLength = endLength;
                    effect.ValueRW.sfxPitch = pitch;
                    effect.ValueRW.sfxVolume = volume;
                    effect.ValueRW.elapsedTime = effect.ValueRO.muzzleFlashDuration;
                    if (effect.ValueRO.isPlayOneShot == false) effect.ValueRW.isPlayOneShot = true;
                    effect.ValueRW.SpawnPosition = spawnLocalTransform.Position;
                    effect.ValueRW.SpawnRandomRotation = spawnLocalTransform.RotateZ(randomZ).Rotation;
                    SystemAPI.GetComponentRW<Parent>(entityEffect).ValueRW.Value = tip.barrelTipEntity;
                    barrelAnimator.ValueRW.random = random;
                    RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot = SystemAPI.GetComponentRW<SoundWeaponEffectShoot>(pointShoot);
                    soundWeaponEffectShoot.ValueRW.pitch = barrelAnimator.ValueRO.sfxPitch;
                    soundWeaponEffectShoot.ValueRW.volume = barrelAnimator.ValueRO.sfxVolume;
                    soundWeaponEffectShoot.ValueRW.isPlayOneShot = true;
                    barrelAnimator.ValueRW.flashSpawned = true;
                }
            }
            else if (weapon.ValueRO.firingPattern == Enum.WeaponFiringPattern.Simultaneous || weapon.ValueRO.firingPattern == Enum.WeaponFiringPattern.Gatling)
            {
                for (int index = 0; index < tipBuffers.Length; index++)
                {
                    BarrelTipEntityBuffer tip = tipBuffers[index];
                    PointShotEntityBuffer pointShotEntityBuffer = pointShotBuffers[index];
                    RefRW<LocalTransform> tipTransform = SystemAPI.GetComponentRW<LocalTransform>(tip.barrelTipEntity);

                    if (tip.tipInitialPosition.Equals(float3.zero) && tip.tipInitialRotation.Equals(float3.zero))
                    {
                        tip.tipInitialPosition = tipTransform.ValueRO.Position;
                        tip.tipInitialRotation = math.Euler(tipTransform.ValueRO.Rotation);
                        tipBuffers.ElementAt(index) = tip;
                    }

                    float tipY = tip.tipInitialPosition.y + slideValue * barrelAnimator.ValueRO.tipSlideAmountDistance;

                    tipTransform.ValueRW.Position = new float3(
                        tip.tipInitialPosition.x,
                        tipY,
                        tip.tipInitialPosition.z
                    );

                    if (barrelAnimator.ValueRO.tipRotateDegrees != 0f)
                    {
                        float tipRotY = tip.tipInitialRotation.y;
                        tipRotY = math.lerp(barrelAnimator.ValueRO.tipRotationAtFire,
                            barrelAnimator.ValueRO.tipRotationAtFire + barrelAnimator.ValueRO.tipRotateDegrees,
                            rotationValue);
                        tipTransform.ValueRW.Rotation = quaternion.Euler(
                            math.radians(tip.tipInitialRotation.x),
                            math.radians(tipRotY),
                            math.radians(tip.tipInitialRotation.z)
                        );
                    }

                    if (!barrelAnimator.ValueRO.flashSpawned)
                    {
                        Entity pointShoot = pointShotEntityBuffer.pointShoot;
                        LocalTransform spawnLocalTransform = SystemAPI.GetComponent<LocalTransform>(pointShoot);
                        Unity.Mathematics.Random random = barrelAnimator.ValueRO.random;
                        Entity entityEffect = barrelAnimator.ValueRO.muzzleFlashEntity;
                        RefRW<LocalToWorld> localToWorld = SystemAPI.GetComponentRW<LocalToWorld>(entityEffect);
                        RefRW<EffectWeaponShoot> effect = SystemAPI.GetComponentRW<EffectWeaponShoot>(entityEffect);
                        float startScale = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.scaleVariance / 2f;
                        float endScale = startScale * random.NextFloat(0.6f, 0.8f);
                        float startLength = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.lengthVariance / 2f;
                        float endLength = startLength * random.NextFloat(1.75f, 3f);
                        float randomZ = random.NextFloat(-180f, 180f);
                        float pitch = math.clamp(barrelAnimator.ValueRO.sfxPitch + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
                        float volume = math.clamp(barrelAnimator.ValueRO.sfxVolume + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
                        effect.ValueRW.startScale = startScale;
                        effect.ValueRW.endScale = endScale;
                        effect.ValueRW.startLength = startLength;
                        effect.ValueRW.endLength = endLength;
                        effect.ValueRW.sfxPitch = pitch;
                        effect.ValueRW.sfxVolume = volume;
                        effect.ValueRW.elapsedTime = effect.ValueRO.muzzleFlashDuration;
                        if (effect.ValueRO.isPlayOneShot == false) effect.ValueRW.isPlayOneShot = true;
                        effect.ValueRW.SpawnPosition = spawnLocalTransform.Position;
                        effect.ValueRW.SpawnRandomRotation = spawnLocalTransform.RotateZ(randomZ).Rotation;
                        SystemAPI.GetComponentRW<Parent>(entityEffect).ValueRW.Value = tip.barrelTipEntity;
                        barrelAnimator.ValueRW.random = random;
                        RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot = SystemAPI.GetComponentRW<SoundWeaponEffectShoot>(pointShoot);
                        soundWeaponEffectShoot.ValueRW.pitch = barrelAnimator.ValueRO.sfxPitch;
                        soundWeaponEffectShoot.ValueRW.volume = barrelAnimator.ValueRO.sfxVolume;
                        soundWeaponEffectShoot.ValueRW.isPlayOneShot = true;
                        barrelAnimator.ValueRW.flashSpawned = true;
                    }
                }
            }
            if (progress >= 1f)
            {
                barrelAnimator.ValueRW.animationPlaying = false;
                barrelAnimator.ValueRW.flashSpawned = false;
            }
        }
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
