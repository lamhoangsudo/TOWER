using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
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

            switch (weapon.ValueRO.firingPattern)
            {
                case Enum.WeaponFiringPattern.MissileLauncher:
                case Enum.WeaponFiringPattern.Individual:
                    if (!barrelAnimator.ValueRO.animationPlaying) break;
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
                    if (progress >= 1f)
                    {
                        barrelAnimator.ValueRW.animationPlaying = false;
                        barrelAnimator.ValueRW.flashSpawned = false;
                    }
                    break;
                case Enum.WeaponFiringPattern.Simultaneous:
                    if (!barrelAnimator.ValueRO.animationPlaying) continue;
                    float elapsedSimultaneous = (float)SystemAPI.Time.ElapsedTime - barrelAnimator.ValueRO.lastFireTime;
                    float progressSimultaneous = math.clamp(elapsedSimultaneous / barrelAnimator.ValueRO.animationDuration, 0f, 1f);
                    ref BarrelAnimatorCurveBlob blobSimultaneous = ref barrelAnimator.ValueRO.curveBlob.Value;
                    int sampleCountSimultaneous = blobSimultaneous.sampleCount;
                    float sampleTSimultaneous = progressSimultaneous * (sampleCountSimultaneous - 1);
                    int idx0Simultaneous = (int)math.floor(sampleTSimultaneous);
                    int idx1Simultaneous = math.min(idx0Simultaneous + 1, sampleCountSimultaneous - 1);
                    float fracSimultaneous = sampleTSimultaneous - idx0Simultaneous;
                    float slideValueSimultaneous = math.lerp(blobSimultaneous.slideCurve[idx0Simultaneous], blobSimultaneous.slideCurve[idx1Simultaneous], fracSimultaneous);
                    float rotationValueSimultaneous = math.lerp(blobSimultaneous.rotationCurve[idx0Simultaneous], blobSimultaneous.rotationCurve[idx0Simultaneous], fracSimultaneous);
                    if (barrelAnimator.ValueRO.barrelBaseEntity != Entity.Null && SystemAPI.HasComponent<LocalTransform>(barrelAnimator.ValueRO.barrelBaseEntity))
                    {
                        RefRW<LocalTransform> baseTransform = SystemAPI.GetComponentRW<LocalTransform>(barrelAnimator.ValueRO.barrelBaseEntity);
                        float3 basePos = new(0f, 0f, -slideValueSimultaneous * barrelAnimator.ValueRO.baseSlideDistance);
                        baseTransform.ValueRW.Position = basePos;
                    }
                    for (int index = 0; index < tipBuffers.Length; index++)
                    {
                        BarrelTipEntityBuffer tipSimultaneous = tipBuffers[index];
                        PointShotEntityBuffer pointShotEntityBufferSimultaneous = pointShotBuffers[index];
                        RefRW<LocalTransform> tipTransformSimultaneous = SystemAPI.GetComponentRW<LocalTransform>(tipSimultaneous.barrelTipEntity);
                        if (tipSimultaneous.tipInitialPosition.Equals(float3.zero) && tipSimultaneous.tipInitialRotation.Equals(float3.zero))
                        {
                            tip.tipInitialPosition = tipTransformSimultaneous.ValueRO.Position;
                            tip.tipInitialRotation = math.Euler(tipTransformSimultaneous.ValueRO.Rotation);
                            tipBuffers.ElementAt(index) = tipSimultaneous;
                        }
                        float tipYSimultaneous = tipSimultaneous.tipInitialPosition.y + slideValueSimultaneous * barrelAnimator.ValueRO.tipSlideAmountDistance;
                        tipTransformSimultaneous.ValueRW.Position = new float3(
                            tipSimultaneous.tipInitialPosition.x,
                            tipYSimultaneous,
                            tipSimultaneous.tipInitialPosition.z
                        );
                        if (barrelAnimator.ValueRO.tipRotateDegrees != 0f)
                        {
                            float tipRotYSimultaneous = tipSimultaneous.tipInitialRotation.y;
                            tipRotYSimultaneous = math.lerp(barrelAnimator.ValueRO.tipRotationAtFire,
                                barrelAnimator.ValueRO.tipRotationAtFire + barrelAnimator.ValueRO.tipRotateDegrees,
                                rotationValueSimultaneous);
                            tipTransformSimultaneous.ValueRW.Rotation = quaternion.Euler(
                                math.radians(tipSimultaneous.tipInitialRotation.x),
                                math.radians(tipRotYSimultaneous),
                                math.radians(tipSimultaneous.tipInitialRotation.z)
                            );
                        }
                        if (!barrelAnimator.ValueRO.flashSpawned)
                        {
                            Entity pointShoot = pointShotEntityBufferSimultaneous.pointShoot;
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
                            SystemAPI.GetComponentRW<Parent>(entityEffect).ValueRW.Value = tipSimultaneous.barrelTipEntity;
                            barrelAnimator.ValueRW.random = random;
                            RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot = SystemAPI.GetComponentRW<SoundWeaponEffectShoot>(pointShoot);
                            soundWeaponEffectShoot.ValueRW.pitch = barrelAnimator.ValueRO.sfxPitch;
                            soundWeaponEffectShoot.ValueRW.volume = barrelAnimator.ValueRO.sfxVolume;
                            soundWeaponEffectShoot.ValueRW.isPlayOneShot = true;
                            barrelAnimator.ValueRW.flashSpawned = true;
                        }
                        if (progressSimultaneous >= 1f)
                        {
                            barrelAnimator.ValueRW.animationPlaying = false;
                            barrelAnimator.ValueRW.flashSpawned = false;
                        }
                    }
                    break;
                case Enum.WeaponFiringPattern.Gatling:
                    float gatlingRotationFactor = barrelAnimator.ValueRO.curentGatlingRotation / barrelAnimator.ValueRO.gatlingRotationSpeed;
                    RefRW<LocalTransform> tipTransformGatling = SystemAPI.GetComponentRW<LocalTransform>(tipBuffers[0].barrelTipEntity);
                    tipTransformGatling.ValueRW = tipTransformGatling.ValueRW.WithRotation(quaternion.Euler(0f, math.radians(barrelAnimator.ValueRO.curentGatlingRotation * SystemAPI.Time.DeltaTime), 0f));
                    RefRW<SFX_GatlingSpin> sfx_GatlingSpin = SystemAPI.GetComponentRW<SFX_GatlingSpin>(barrelAnimator.ValueRO.audioGatlingEffect);
                    if (gatlingRotationFactor > 0.05f)
                    {
                        sfx_GatlingSpin.ValueRW.isPlaying = true;
                        sfx_GatlingSpin.ValueRW.gatlingRotationFactor = barrelAnimator.ValueRO.curentGatlingRotation / barrelAnimator.ValueRO.gatlingRotationSpeed;
                        sfx_GatlingSpin.ValueRW.curentGatlingRotation = barrelAnimator.ValueRO.curentGatlingRotation;
                    }
                    else
                    {
                        sfx_GatlingSpin.ValueRW.isPlaying = false;
                    }
                    if (barrelAnimator.ValueRO.animationPlaying)
                    {
                        BarrelTipEntityBuffer tipSimultaneous = tipBuffers[0];
                        PointShotEntityBuffer pointShotEntityBufferSimultaneous = pointShotBuffers[0];
                        Entity pointShoot = pointShotEntityBufferSimultaneous.pointShoot;
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
                        float pitch = barrelAnimator.ValueRO.sfxPitch;
                        float volume = barrelAnimator.ValueRO.sfxVolume;
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
                        SystemAPI.GetComponentRW<Parent>(entityEffect).ValueRW.Value = tipSimultaneous.barrelTipEntity;
                        barrelAnimator.ValueRW.random = random;
                        RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot = SystemAPI.GetComponentRW<SoundWeaponEffectShoot>(pointShoot);
                        soundWeaponEffectShoot.ValueRW.pitch = barrelAnimator.ValueRO.sfxPitch;
                        soundWeaponEffectShoot.ValueRW.volume = barrelAnimator.ValueRO.sfxVolume;
                        soundWeaponEffectShoot.ValueRW.isPlayOneShot = true;
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
