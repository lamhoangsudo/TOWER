using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Spawn muzzle flash + fire sound + projectile trigger khi barrel animation bắt đầu.
/// Chạy sau BarrelSlideAnimSystem.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(BarrelSlideAnimSystem))]
[UpdateAfter(typeof(BarrelGatlingSpinSystem))]
partial struct BarrelFireEffectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<BarrelAnimation, BarrelVFX, BarrelSFX, Weapon, BarrelTipEntityBuffer, WeaponFireTime>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        BarrelFireEffectJob job = new()
        {
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            effectWeaponShootLookup = SystemAPI.GetComponentLookup<EffectWeaponShoot>(isReadOnly: false),
            parentLookup = SystemAPI.GetComponentLookup<Parent>(isReadOnly: false),
            soundWeaponEffectShootLookup = SystemAPI.GetComponentLookup<SoundWeaponEffectShoot>(isReadOnly: false),
            projectileSpawnShootLookup = SystemAPI.GetComponentLookup<ProjectileSpawnShoot>(isReadOnly: false),
            ecb = ecb.AsParallelWriter(),
        };
        JobHandle jobHandle = job.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct BarrelFireEffectJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<EffectWeaponShoot> effectWeaponShootLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<Parent> parentLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<SoundWeaponEffectShoot> soundWeaponEffectShootLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<ProjectileSpawnShoot> projectileSpawnShootLookup;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [ChunkIndexInQuery] int sortkey,
        ref BarrelAnimation barrelAnimation,
        ref BarrelVFX barrelVFX,
        ref BarrelSFX barrelSFX,
        in Weapon weapon,
        DynamicBuffer<BarrelTipEntityBuffer> tipBuffers,
        in WeaponFireTime weaponFireTime)
    {
        if (!barrelAnimation.animationPlaying) return;
        if (barrelVFX.flashSpawned) return;

        ref BlobArray<PointShotEntityBlobData> pointShotBuffers = ref barrelVFX.pointShotBlob.Value.pointShotEntityBlobDataArray;

        switch (weapon.firingPattern)
        {
            case Enum.WeaponFiringPattern.Individual:
            case Enum.WeaponFiringPattern.MissileLauncher:
            case Enum.WeaponFiringPattern.Gatling:
                int pointIndex = math.clamp(weaponFireTime.pointShootIndex, 0, pointShotBuffers.Length - 1);
                int tipIndex = math.clamp(weaponFireTime.barrelTipIndex, 0, tipBuffers.Length - 1);
                SpawnEffects(sortkey, ref barrelVFX, ref barrelSFX, weapon, tipBuffers[tipIndex], pointShotBuffers[pointIndex]);
                break;

            case Enum.WeaponFiringPattern.Simultaneous:
                for (int i = 0; i < tipBuffers.Length && i < pointShotBuffers.Length; i++)
                {
                    SpawnEffects(sortkey, ref barrelVFX, ref barrelSFX, weapon, tipBuffers[i], pointShotBuffers[i]);
                }
                break;
        }

        barrelVFX.flashSpawned = true;
    }

    private void SpawnEffects(
        int sortkey,
        ref BarrelVFX barrelVFX,
        ref BarrelSFX barrelSFX,
        Weapon weapon,
        BarrelTipEntityBuffer tip,
        PointShotEntityBlobData pointShotData)
    {
        Entity pointShoot = pointShotData.pointShoot;
        if (!localTransformLookup.HasComponent(pointShoot)) return;

        LocalTransform spawnLocalTransform = localTransformLookup[pointShoot];

        // Randomize SFX
        Unity.Mathematics.Random random = barrelSFX.random;
        float randomZ = random.NextFloat(-180f, 180f);
        float pitch = math.clamp(barrelSFX.sfxPitch + random.NextFloat(-0.125f, 0.125f), 0.2f, 4f);
        float volume = math.clamp(barrelSFX.sfxVolume + random.NextFloat(-0.125f, 0.125f), 0.2f, 4f);
        barrelSFX.random = random;

        // Muzzle flash
        Entity effectEntity = barrelVFX.muzzleFlashEntity;
        if (effectWeaponShootLookup.HasComponent(effectEntity))
        {
            EffectWeaponShoot effect = effectWeaponShootLookup[effectEntity];
            effect.sfxPitch = pitch;
            effect.sfxVolume = volume;
            effect.elapsedTime = effect.muzzleFlashDuration;
            effect.isPlayOneShot = true;
            effect.SpawnPosition = spawnLocalTransform.Position;
            effect.SpawnRandomRotation = spawnLocalTransform.RotateZ(randomZ).Rotation;
            ecb.SetComponent(sortkey, effectEntity, effect);

            // Re-parent flash to current tip
            if (parentLookup.HasComponent(effectEntity))
            {
                Parent parent = parentLookup[effectEntity];
                parent.Value = tip.barrelTipEntity;
                ecb.SetComponent(sortkey, effectEntity, parent);
            }
        }

        // Fire sound
        if (soundWeaponEffectShootLookup.HasComponent(pointShoot))
        {
            SoundWeaponEffectShoot sound = soundWeaponEffectShootLookup[pointShoot];
            sound.pitch = pitch;
            sound.volume = volume;
            sound.isPlayOneShot = true;
            ecb.SetComponent(sortkey, pointShoot, sound);
        }

        // Projectile spawn trigger
        if (projectileSpawnShootLookup.HasComponent(pointShoot))
        {
            ProjectileSpawnShoot spawnTrigger = projectileSpawnShootLookup[pointShoot];
            if (weapon.targetEntity != Entity.Null && localTransformLookup.HasComponent(weapon.targetEntity))
            {
                spawnTrigger.targetPosition = localTransformLookup[weapon.targetEntity].Position;
            }
            if (weapon.firingPattern == Enum.WeaponFiringPattern.MissileLauncher)
            {
                spawnTrigger.homingTarget = weapon.targetEntity;
            }
            spawnTrigger.firingPattern = weapon.firingPattern;
            spawnTrigger.entityProjectilePrefab = weapon.projectilePrefab;
            spawnTrigger.entityProjectileExplosion = weapon.explosionPrefab;
            spawnTrigger.projectileLifetimeMax = weapon.projectileMaxLifetime;
            spawnTrigger.projectileStartSpeed = weapon.projectileStartSpeed;
            spawnTrigger.projectileMaxSpeed = weapon.projectileMaxSpeed;
            spawnTrigger.projectileAcceleration = weapon.projectileAcceleration;
            spawnTrigger.isSpawner = true;
            ecb.SetComponent(sortkey, pointShoot, spawnTrigger);
        }
    }
}
