using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretPitchSystem))]
partial struct TurretFireSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        /*
        foreach ((RefRO<Turret> turret, DynamicBuffer<WeaponBuffer> weaponBuffers, RefRW<TurretFireTime> turretFireTime) in SystemAPI.Query<RefRO<Turret>, DynamicBuffer<WeaponBuffer>, RefRW<TurretFireTime>>())
        {
            turretFireTime.ValueRW.cooldown -= SystemAPI.Time.DeltaTime;
            if (turretFireTime.ValueRO.cooldown > 0) continue;

            if (turret.ValueRO.autoFire && turret.ValueRO.isHeadingRotationTarget && turret.ValueRO.isElevationRotationTarget)
            {
                shouldFire = true;
            }
            else
            {
                shouldFire = false;
            }

            RefRW<Weapon> weapon;
            if (weaponBuffers.Length > 1)
            {
                switch (turretFireTime.ValueRO.firingPattern)
                {
                    case Enum.TurretFiringPattern.Simultaneous:
                        if (turretFireTime.ValueRO.burstCount >= turretFireTime.ValueRO.burstCountMax)
                        {
                            turretFireTime.ValueRW.cooldown = turretFireTime.ValueRO.cooldownMax;
                            turretFireTime.ValueRW.burstCount = 0;
                            break;
                        }
                        turretFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                        if (turretFireTime.ValueRO.burstDelay < turretFireTime.ValueRO.burstDelayMax) break;
                        foreach (WeaponBuffer weaponBuffer in weaponBuffers)
                        {
                            weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffer.weaponBuffer);
                            if (weapon.ValueRO.startFire == shouldFire) continue;
                            turretFireTime.ValueRW.burstCount = math.clamp(turretFireTime.ValueRO.burstCount, 0, turretFireTime.ValueRO.burstCountMax);
                            weapon.ValueRW.startFire = shouldFire;
                            turretFireTime.ValueRW.burstCount ++;
                            UnityEngine.Debug.Log("Fire Simultaneous");
                        }
                        
                        turretFireTime.ValueRW.burstDelay = 0f;
                        break;
                    case Enum.TurretFiringPattern.Individual:
                        if (turretFireTime.ValueRO.burstCount >= turretFireTime.ValueRO.burstCountMax)
                        {
                            turretFireTime.ValueRW.cooldown = turretFireTime.ValueRO.cooldownMax;
                            turretFireTime.ValueRW.burstCount = 0;
                            break;
                        }
                        turretFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                        if (turretFireTime.ValueRO.burstDelay < turretFireTime.ValueRO.burstDelayMax) break;
                        weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffers[indexWeapons].weaponBuffer);
                        if (weapon.ValueRO.startFire == shouldFire) break;
                        indexWeapons++;
                        if (indexWeapons >= weaponBuffers.Length) indexWeapons = 0;
                        turretFireTime.ValueRW.burstCount++;
                        turretFireTime.ValueRW.burstCount = math.clamp(turretFireTime.ValueRO.burstCount, 0, turretFireTime.ValueRO.burstCountMax);
                        turretFireTime.ValueRW.burstDelay = 0f;
                        weapon.ValueRW.startFire = shouldFire;
                        break;
                }
            }
            else if (weaponBuffers.Length == 1)
            {
                weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffers[0].weaponBuffer);
                if (weapon.ValueRO.startFire != shouldFire)
                {
                    weapon.ValueRW.startFire = shouldFire;
                }
            }
        }
        */
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        TurretFireJob turretFireJob = new TurretFireJob()
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
            componentLookupWeapon = SystemAPI.GetComponentLookup<Weapon>(isReadOnly: false),
        };
        turretFireJob.ScheduleParallel();
        state.Dependency.Complete();
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
        public void Execute(in Turret turret, ref DynamicBuffer<WeaponBuffer> weaponBuffers, ref TurretFireTime turretFireTime, [ChunkIndexInQuery] int sortkey)
        {
            turretFireTime.cooldown -= deltaTime;
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
                    ecb.SetComponent<Weapon>(sortkey, weaponBuffers[turretFireTime.indexWeapons].weaponBuffer, weaponWritter);
                    break;
                case Enum.TurretFiringPattern.Gatling:
                    foreach (WeaponBuffer weaponBuffer in weaponBuffers)
                    {
                        weaponWritter = componentLookupWeapon[weaponBuffer.weaponBuffer];
                        if (weaponWritter.startFire == shouldFire) continue;
                        weaponWritter.startFire = shouldFire;
                        ecb.SetComponent<Weapon>(sortkey, weaponBuffer.weaponBuffer, weaponWritter);
                    }
                    break;
            }
            return;
        }
    }
}
