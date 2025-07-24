using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
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
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        BarrelAnimatorSystemJob barrelAnimatorSystemJob = new()
        {
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
            DeltaTime = SystemAPI.Time.DeltaTime,
            _LocalTransformLookUp = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            _EffectWeaponShootLookUp = SystemAPI.GetComponentLookup<EffectWeaponShoot>(isReadOnly: false),
            _ParentLookUp = SystemAPI.GetComponentLookup<Parent>(isReadOnly: false),
            _SoundWeaponEffectShootLookUp = SystemAPI.GetComponentLookup<SoundWeaponEffectShoot>(isReadOnly: false),
            _SFX_GatlingSpinLookUp = SystemAPI.GetComponentLookup<SFX_GatlingSpin>(isReadOnly: false),
            _ProjectTileSpawnShoot = SystemAPI.GetComponentLookup<ProjectTileSpawnShoot>(isReadOnly: false),
            ecb = ecb.AsParallelWriter(),
        };
        JobHandle jobHandle = barrelAnimatorSystemJob.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct BarrelAnimatorSystemJob : IJobEntity
    {
        public float ElapsedTime;
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> _LocalTransformLookUp;
        [ReadOnly] public ComponentLookup<EffectWeaponShoot> _EffectWeaponShootLookUp;
        [ReadOnly] public ComponentLookup<ProjectTileSpawnShoot> _ProjectTileSpawnShoot;
        [ReadOnly] public ComponentLookup<Parent> _ParentLookUp;
        [ReadOnly] public ComponentLookup<SoundWeaponEffectShoot> _SoundWeaponEffectShootLookUp;
        [ReadOnly] public ComponentLookup<SFX_GatlingSpin> _SFX_GatlingSpinLookUp;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(
            [ChunkIndexInQuery] int sortkey,
            ref BarrelAnimator barrelAnimator,
            in Weapon weapon,
            DynamicBuffer<BarrelTipEntityBuffer> tipBuffers,
            DynamicBuffer<PointShotEntityBuffer> pointShotBuffers,
            in WeaponFireTime weaponFireTime
            )
        {
            switch (weapon.firingPattern)
            {
                case Enum.WeaponFiringPattern.MissileLauncher:
                case Enum.WeaponFiringPattern.Individual:
                    if (!barrelAnimator.animationPlaying) break;
                    /*
                    float elapsed = ElapsedTime - barrelAnimator.lastFireTime;
                    float progress = math.clamp(elapsed / barrelAnimator.animationDuration, 0f, 1f);
                    ref BarrelAnimatorCurveBlob blob = ref barrelAnimator.curveBlob.Value;
                    int sampleCount = blob.sampleCount;
                    float sampleT = progress * (sampleCount - 1);
                    int idx0 = (int)math.floor(sampleT);
                    int idx1 = math.min(idx0 + 1, sampleCount - 1);
                    float frac = sampleT - idx0;
                    float slideValue = math.lerp(blob.slideCurve[idx0], blob.slideCurve[idx1], frac);
                    float rotationValue = math.lerp(blob.rotationCurve[idx0], blob.rotationCurve[idx1], frac);
                    */
                    CaculatorSlideValueRotationValueProgress(ElapsedTime, barrelAnimator, out float progress, out float slideValue, out float rotationValue);
                    LocalTransform baseTransformWritter = _LocalTransformLookUp[barrelAnimator.barrelBaseEntity];
                    if (barrelAnimator.barrelBaseEntity != Entity.Null)
                    {
                        float3 basePos = new(0f, 0f, -slideValue * barrelAnimator.baseSlideDistance);
                        baseTransformWritter.Position = basePos;
                    }
                    BarrelTipEntityBuffer tip = tipBuffers[weaponFireTime.barrelTipIndex];
                    PointShotEntityBuffer pointShotEntityBuffer = pointShotBuffers[weaponFireTime.pointShootIndex];
                    LocalTransform tipTransformWritter = _LocalTransformLookUp[tip.barrelTipEntity];
                    if (tip.tipInitialPosition.Equals(float3.zero) && tip.tipInitialRotation.Equals(float3.zero))
                    {
                        tip.tipInitialPosition = tipTransformWritter.Position;
                        tip.tipInitialRotation = math.Euler(tipTransformWritter.Rotation);
                        tipBuffers.ElementAt(weaponFireTime.barrelTipIndex) = tip;
                    }
                    float tipY = tip.tipInitialPosition.y + slideValue * barrelAnimator.tipSlideAmountDistance;
                    tipTransformWritter.Position = new float3(
                        tip.tipInitialPosition.x,
                        tipY,
                        tip.tipInitialPosition.z
                    );
                    if (barrelAnimator.tipRotateDegrees != 0f)
                    {
                        float tipRotY = tip.tipInitialRotation.y;
                        tipRotY = math.lerp(barrelAnimator.tipRotationAtFire,
                            barrelAnimator.tipRotationAtFire + barrelAnimator.tipRotateDegrees,
                            rotationValue);
                        tipTransformWritter.Rotation = quaternion.Euler(
                            math.radians(tip.tipInitialRotation.x),
                            math.radians(tipRotY),
                            math.radians(tip.tipInitialRotation.z)
                        );
                    }
                    if (!barrelAnimator.flashSpawned)
                    {
                        Entity pointShoot = pointShotEntityBuffer.pointShoot;
                        LocalTransform spawnLocalTransform = _LocalTransformLookUp[pointShoot];
                        Random random = barrelAnimator.random;
                        Entity entityEffect = barrelAnimator.muzzleFlashEntity;
                        EffectWeaponShoot effectWritter = _EffectWeaponShootLookUp[entityEffect];
                        effectWritter = WritterEffectWeaponShootData(barrelAnimator, effectWritter, spawnLocalTransform);
                        Parent parentEntityEffectWritter = _ParentLookUp[entityEffect];
                        parentEntityEffectWritter.Value = tip.barrelTipEntity;
                        barrelAnimator.random = random;
                        SoundWeaponEffectShoot soundWeaponEffectShootWritter = _SoundWeaponEffectShootLookUp[pointShoot];
                        soundWeaponEffectShootWritter.pitch = barrelAnimator.sfxPitch;
                        soundWeaponEffectShootWritter.volume = barrelAnimator.sfxVolume;
                        soundWeaponEffectShootWritter.isPlayOneShot = true;
                        barrelAnimator.flashSpawned = true;
                        float3 targetPosition = _LocalTransformLookUp[weapon.targetEntity].Position;
                        ProjectTileSpawnShoot projectTileSpawnShootWritter = _ProjectTileSpawnShoot[pointShoot];
                        if(weapon.firingPattern == Enum.WeaponFiringPattern.MissileLauncher)
                        {
                            projectTileSpawnShootWritter.homingTarget = weapon.targetEntity;
                        }
                        projectTileSpawnShootWritter.targetPosition = targetPosition;
                        projectTileSpawnShootWritter.firingPattern = weapon.firingPattern;
                        projectTileSpawnShootWritter.entityProjectTilePrefab = weapon.projectilePrefab;
                        projectTileSpawnShootWritter.entityProjectTileExplosion = weapon.explosionPrefab;
                        projectTileSpawnShootWritter.projectileLifetimeMax = weapon.projectileMaxLifetime;
                        projectTileSpawnShootWritter.projectileStartSpeed = weapon.projectileStartSpeed;
                        projectTileSpawnShootWritter.projectileMaxSpeed = weapon.projectileMaxSpeed;
                        projectTileSpawnShootWritter.projectileAcceleration = weapon.projectileAcceleration;
                        projectTileSpawnShootWritter.projectileStartSpeed = weapon.projectileStartSpeed;
                        projectTileSpawnShootWritter.isSpawner = true;

                        ecb.SetComponent<EffectWeaponShoot>(sortkey, entityEffect, effectWritter);
                        ecb.SetComponent<Parent>(sortkey, entityEffect, parentEntityEffectWritter);
                        ecb.SetComponent<SoundWeaponEffectShoot>(sortkey, pointShoot, soundWeaponEffectShootWritter);
                        ecb.SetComponent<ProjectTileSpawnShoot>(sortkey, pointShoot, projectTileSpawnShootWritter);
                    }
                    if (progress >= 1f)
                    {
                        barrelAnimator.animationPlaying = false;
                        barrelAnimator.flashSpawned = false;
                    }
                    ecb.SetComponent<LocalTransform>(sortkey, barrelAnimator.barrelBaseEntity, baseTransformWritter);
                    ecb.SetComponent<LocalTransform>(sortkey, tip.barrelTipEntity, tipTransformWritter);
                    break;
                case Enum.WeaponFiringPattern.Simultaneous:
                    if (!barrelAnimator.animationPlaying) break;
                    CaculatorSlideValueRotationValueProgress(ElapsedTime, barrelAnimator, out float progressSimultaneous, out float slideValueSimultaneous, out float rotationValueSimultaneous);
                    LocalTransform baseTransformSimultaneousWritter = _LocalTransformLookUp[barrelAnimator.barrelBaseEntity];
                    if (barrelAnimator.barrelBaseEntity != Entity.Null)
                    {
                        float3 basePos = new(0f, 0f, -slideValueSimultaneous * barrelAnimator.baseSlideDistance);
                        baseTransformSimultaneousWritter.Position = basePos;
                    }
                    for (int index = 0; index < tipBuffers.Length; index++)
                    {
                        BarrelTipEntityBuffer tipSimultaneous = tipBuffers[index];
                        PointShotEntityBuffer pointShotEntityBufferSimultaneous = pointShotBuffers[index];
                        LocalTransform tipTransformSimultaneousWritter = _LocalTransformLookUp[tipSimultaneous.barrelTipEntity];
                        if (tipSimultaneous.tipInitialPosition.Equals(float3.zero) && tipSimultaneous.tipInitialRotation.Equals(float3.zero))
                        {
                            tip.tipInitialPosition = tipTransformSimultaneousWritter.Position;
                            tip.tipInitialRotation = math.Euler(tipTransformSimultaneousWritter.Rotation);
                            tipBuffers.ElementAt(index) = tipSimultaneous;
                        }
                        float tipYSimultaneous = tipSimultaneous.tipInitialPosition.y + slideValueSimultaneous * barrelAnimator.tipSlideAmountDistance;
                        tipTransformSimultaneousWritter.Position = new float3(
                            tipSimultaneous.tipInitialPosition.x,
                            tipYSimultaneous,
                            tipSimultaneous.tipInitialPosition.z
                        );
                        if (barrelAnimator.tipRotateDegrees != 0f)
                        {
                            float tipRotYSimultaneous = tipSimultaneous.tipInitialRotation.y;
                            tipRotYSimultaneous = math.lerp(barrelAnimator.tipRotationAtFire,
                                barrelAnimator.tipRotationAtFire + barrelAnimator.tipRotateDegrees,
                                rotationValueSimultaneous);
                            tipTransformSimultaneousWritter.Rotation = quaternion.Euler(
                                math.radians(tipSimultaneous.tipInitialRotation.x),
                                math.radians(tipRotYSimultaneous),
                                math.radians(tipSimultaneous.tipInitialRotation.z)
                            );
                        }
                        if (!barrelAnimator.flashSpawned)
                        {
                            Entity pointShoot = pointShotEntityBufferSimultaneous.pointShoot;
                            LocalTransform spawnLocalTransform = _LocalTransformLookUp[pointShoot];
                            Random random = barrelAnimator.random;
                            Entity entityEffect = barrelAnimator.muzzleFlashEntity;
                            EffectWeaponShoot effectWritter = _EffectWeaponShootLookUp[entityEffect];
                            effectWritter = WritterEffectWeaponShootData(barrelAnimator, effectWritter, spawnLocalTransform);
                            Parent parentEntityEffectWritter = _ParentLookUp[entityEffect];
                            parentEntityEffectWritter.Value = tipSimultaneous.barrelTipEntity;
                            barrelAnimator.random = random;
                            SoundWeaponEffectShoot soundWeaponEffectShootWritter = _SoundWeaponEffectShootLookUp[pointShoot];
                            soundWeaponEffectShootWritter.pitch = barrelAnimator.sfxPitch;
                            soundWeaponEffectShootWritter.volume = barrelAnimator.sfxVolume;
                            soundWeaponEffectShootWritter.isPlayOneShot = true;
                            barrelAnimator.flashSpawned = true;
                            ecb.SetComponent<EffectWeaponShoot>(sortkey, entityEffect, effectWritter);
                            ecb.SetComponent<Parent>(sortkey, entityEffect, parentEntityEffectWritter);
                            ecb.SetComponent<SoundWeaponEffectShoot>(sortkey, pointShoot, soundWeaponEffectShootWritter);
                        }
                        ecb.SetComponent<LocalTransform>(sortkey, tipSimultaneous.barrelTipEntity, tipTransformSimultaneousWritter);
                    }
                    if (progressSimultaneous >= 1f)
                    {
                        barrelAnimator.animationPlaying = false;
                        barrelAnimator.flashSpawned = false;
                    }
                    ecb.SetComponent<LocalTransform>(sortkey, barrelAnimator.barrelBaseEntity, baseTransformSimultaneousWritter);
                    break;
                case Enum.WeaponFiringPattern.Gatling:
                    float gatlingRotationFactor = barrelAnimator.curentGatlingRotation / barrelAnimator.gatlingRotationSpeed;
                    LocalTransform tipTransformGatlingWritter = _LocalTransformLookUp[tipBuffers[0].barrelTipEntity];
                    barrelAnimator.accumulatedGatlingAngle += barrelAnimator.curentGatlingRotation * DeltaTime;
                    tipTransformGatlingWritter = tipTransformGatlingWritter.WithRotation(quaternion.Euler(0f, math.radians(math.fmod(barrelAnimator.accumulatedGatlingAngle, 1800)), 0f));
                    SFX_GatlingSpin sfx_GatlingSpinWritter = _SFX_GatlingSpinLookUp[barrelAnimator.audioGatlingEffect];
                    if (gatlingRotationFactor > 0.05f)
                    {
                        sfx_GatlingSpinWritter.isPlaying = true;
                        sfx_GatlingSpinWritter.gatlingRotationFactor = barrelAnimator.curentGatlingRotation / barrelAnimator.gatlingRotationSpeed;
                    }
                    else
                    {
                        sfx_GatlingSpinWritter.isPlaying = false;
                    }
                    if (barrelAnimator.animationPlaying)
                    {
                        BarrelTipEntityBuffer tipSimultaneous = tipBuffers[0];
                        PointShotEntityBuffer pointShotEntityBufferSimultaneous = pointShotBuffers[0];
                        Entity pointShoot = pointShotEntityBufferSimultaneous.pointShoot;
                        LocalTransform spawnLocalTransform = _LocalTransformLookUp[pointShoot];
                        Random random = barrelAnimator.random;
                        Entity entityEffect = barrelAnimator.muzzleFlashEntity;
                        EffectWeaponShoot effectWritter = _EffectWeaponShootLookUp[entityEffect];
                        effectWritter = WritterEffectWeaponShootData(barrelAnimator, effectWritter, spawnLocalTransform);
                        Parent parentEntityEffectWritter = _ParentLookUp[entityEffect];
                        parentEntityEffectWritter.Value = tipSimultaneous.barrelTipEntity;
                        barrelAnimator.random = random;
                        SoundWeaponEffectShoot soundWeaponEffectShootWritter = _SoundWeaponEffectShootLookUp[pointShoot];
                        soundWeaponEffectShootWritter.pitch = barrelAnimator.sfxPitch;
                        soundWeaponEffectShootWritter.volume = barrelAnimator.sfxVolume;
                        soundWeaponEffectShootWritter.isPlayOneShot = true;

                        float3 targetPosition = _LocalTransformLookUp[weapon.targetEntity].Position;
                        ProjectTileSpawnShoot projectTileSpawnShootWritter = _ProjectTileSpawnShoot[pointShoot];
                        projectTileSpawnShootWritter.targetPosition = targetPosition;
                        projectTileSpawnShootWritter.firingPattern = weapon.firingPattern;
                        projectTileSpawnShootWritter.entityProjectTilePrefab = weapon.projectilePrefab;
                        projectTileSpawnShootWritter.projectileLifetimeMax = weapon.projectileMaxLifetime;
                        projectTileSpawnShootWritter.projectileStartSpeed = weapon.projectileStartSpeed;
                        projectTileSpawnShootWritter.projectileMaxSpeed = weapon.projectileMaxSpeed;
                        projectTileSpawnShootWritter.projectileAcceleration = weapon.projectileAcceleration;
                        projectTileSpawnShootWritter.projectileStartSpeed = weapon.projectileStartSpeed;
                        projectTileSpawnShootWritter.isSpawner = true;
                        ecb.SetComponent<EffectWeaponShoot>(sortkey, entityEffect, effectWritter);
                        ecb.SetComponent<Parent>(sortkey, entityEffect, parentEntityEffectWritter);
                        ecb.SetComponent<SoundWeaponEffectShoot>(sortkey, pointShoot, soundWeaponEffectShootWritter);
                        ecb.SetComponent<ProjectTileSpawnShoot>(sortkey, pointShoot, projectTileSpawnShootWritter);
                    }
                    ecb.SetComponent<LocalTransform>(sortkey, tipBuffers[0].barrelTipEntity, tipTransformGatlingWritter);
                    ecb.SetComponent<SFX_GatlingSpin>(sortkey, barrelAnimator.audioGatlingEffect, sfx_GatlingSpinWritter);
                    break;
            }
        }
    }
    public static EffectWeaponShoot WritterEffectWeaponShootData(BarrelAnimator barrelAnimator, EffectWeaponShoot effectWritter, LocalTransform spawnLocalTransform)
    {
        Unity.Mathematics.Random random = barrelAnimator.random;
        float startScale = 1f + random.NextFloat(-1f, 1f) * effectWritter.scaleVariance / 2f;
        float endScale = startScale * random.NextFloat(0.6f, 0.8f);
        float startLength = 1f + random.NextFloat(-1f, 1f) * effectWritter.lengthVariance / 2f;
        float endLength = startLength * random.NextFloat(1.75f, 3f);
        float randomZ = random.NextFloat(-180f, 180f);
        float pitch = math.clamp(barrelAnimator.sfxPitch + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
        float volume = math.clamp(barrelAnimator.sfxVolume + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
        effectWritter.startScale = startScale;
        effectWritter.endScale = endScale;
        effectWritter.startLength = startLength;
        effectWritter.endLength = endLength;
        effectWritter.sfxPitch = pitch;
        effectWritter.sfxVolume = volume;
        effectWritter.elapsedTime = effectWritter.muzzleFlashDuration;
        if (effectWritter.isPlayOneShot == false) effectWritter.isPlayOneShot = true;
        effectWritter.SpawnPosition = spawnLocalTransform.Position;
        effectWritter.SpawnRandomRotation = spawnLocalTransform.RotateZ(randomZ).Rotation;
        return effectWritter;
    }
    public static void CaculatorSlideValueRotationValueProgress(float _ElapsedTime, BarrelAnimator barrelAnimator, out float progress, out float slideValue, out float rotationValue)
    {
        float elapsed = _ElapsedTime - barrelAnimator.lastFireTime;
        progress = math.clamp(elapsed / barrelAnimator.animationDuration, 0f, 1f);
        ref BarrelAnimatorCurveBlob blob = ref barrelAnimator.curveBlob.Value;
        int sampleCount = blob.sampleCount;
        float sampleT = progress * (sampleCount - 1);
        int idx0 = (int)math.floor(sampleT);
        int idx1 = math.min(idx0 + 1, sampleCount - 1);
        float frac = sampleT - idx0;
        slideValue = math.lerp(blob.slideCurve[idx0], blob.slideCurve[idx1], frac);
        rotationValue = math.lerp(blob.rotationCurve[idx0], blob.rotationCurve[idx1], frac);
    }
}
