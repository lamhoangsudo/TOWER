using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretElevationSystem))]
partial struct TurretFireSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        TurretFireJob turretFireJob = new TurretFireJob()
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
            componentLookupWeapon = SystemAPI.GetComponentLookup<Weapon>(isReadOnly: false),
            componentLookupWeaponFireTime = SystemAPI.GetComponentLookup<WeaponFireTime>(isReadOnly: true),
        };
        JobHandle jobHandle = turretFireJob.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct TurretFireJob : IJobEntity
    {
        public float deltaTime;
        private bool shouldFire;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public ComponentLookup<Weapon> componentLookupWeapon;
        [ReadOnly] public ComponentLookup<WeaponFireTime> componentLookupWeaponFireTime;
        public void Execute(in Turret turret, ref DynamicBuffer<WeaponBuffer> weaponBuffers, ref TurretFireTime turretFireTime, [ChunkIndexInQuery] int sortkey)
        {
            if(turretFireTime.burstCount == 0) turretFireTime.cooldown -= deltaTime;
            if (turretFireTime.cooldown > 0) return;

            if (turret.autoFire && turret.isHeadingRotationTarget && turret.isElevationRotationTarget)
            {
                shouldFire = true;
            }
            else
            {
                shouldFire = false;
            }

            Weapon weaponWritter;
            switch (turretFireTime.firingPattern)
            {
                case Enum.TurretFiringPattern.Simultaneous:
                    if (weaponBuffers.Length <= 1) break;
                    if (turretFireTime.burstCount >= turretFireTime.burstCountMax)
                    {
                        turretFireTime.cooldown = turretFireTime.cooldownMax;
                        turretFireTime.burstCount = 0;
                        break;
                    }
                    turretFireTime.burstDelay += deltaTime;
                    if (turretFireTime.burstDelay < turretFireTime.burstDelayMax) break;
                    foreach (WeaponBuffer weaponBuffer in weaponBuffers)
                    {
                        weaponWritter = componentLookupWeapon[weaponBuffer.weaponBuffer];
                        if (weaponWritter.startFire == shouldFire) continue;
                        turretFireTime.burstCount = math.clamp(turretFireTime.burstCount, 0, turretFireTime.burstCountMax);
                        weaponWritter.startFire = shouldFire;
                        turretFireTime.burstCount++;
                        if (weaponWritter.targetEntity == Entity.Null || weaponWritter.targetEntity != turret.target) weaponWritter.targetEntity = turret.target;
                        ecb.SetComponent<Weapon>(sortkey, weaponBuffer.weaponBuffer, weaponWritter);
                    }
                    turretFireTime.burstDelay = 0f;
                    break;
                case Enum.TurretFiringPattern.Individual:
                    if (turretFireTime.burstCount >= turretFireTime.burstCountMax)
                    {
                        turretFireTime.cooldown = turretFireTime.cooldownMax;
                        turretFireTime.burstCount = 0;
                        break;
                    }
                    turretFireTime.burstDelay += deltaTime;
                    if (turretFireTime.burstDelay < turretFireTime.burstDelayMax) break;
                    weaponWritter = componentLookupWeapon[weaponBuffers[turretFireTime.indexWeapons].weaponBuffer];
                    if (weaponWritter.startFire == shouldFire) break;
                    turretFireTime.indexWeapons++;
                    if (turretFireTime.indexWeapons >= weaponBuffers.Length) turretFireTime.indexWeapons = 0;
                    turretFireTime.burstCount++;
                    turretFireTime.burstCount = math.clamp(turretFireTime.burstCount, 0, turretFireTime.burstCountMax);
                    turretFireTime.burstDelay = 0f;
                    weaponWritter.startFire = shouldFire;
                    if (weaponWritter.targetEntity == Entity.Null || weaponWritter.targetEntity != turret.target) weaponWritter.targetEntity = turret.target;
                    ecb.SetComponent<Weapon>(sortkey, weaponBuffers[turretFireTime.indexWeapons].weaponBuffer, weaponWritter);
                    break;
                case Enum.TurretFiringPattern.Gatling:
                    if (turretFireTime.burstCount >= turretFireTime.burstCountMax)
                    {
                        turretFireTime.cooldown = turretFireTime.cooldownMax + componentLookupWeaponFireTime[weaponBuffers[0].weaponBuffer].timeOverHeatMax;
                        turretFireTime.burstCount = 0;
                    }
                    else
                    {
                        turretFireTime.burstDelay += deltaTime;
                        if (turretFireTime.burstDelay < turretFireTime.burstDelayMax) break;
                        foreach (WeaponBuffer weaponBuffer in weaponBuffers)
                        {
                            weaponWritter = componentLookupWeapon[weaponBuffer.weaponBuffer];
                            if (weaponWritter.startFire == shouldFire) continue;
                            turretFireTime.burstCount++;
                            weaponWritter.startFire = shouldFire;
                            if (weaponWritter.targetEntity == Entity.Null || weaponWritter.targetEntity != turret.target) weaponWritter.targetEntity = turret.target;
                            ecb.SetComponent<Weapon>(sortkey, weaponBuffer.weaponBuffer, weaponWritter);
                        }
                    }
                    turretFireTime.burstDelay = 0f;
                    break;
            }
            return;
        }
    }
}
