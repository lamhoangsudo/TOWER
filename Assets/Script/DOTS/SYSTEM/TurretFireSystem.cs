using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretPitchSystem))]
partial struct TurretFireSystem : ISystem
{
    private bool shouldFire;
    private int indexWeapons;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
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
                        turretFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                        if (turretFireTime.ValueRO.burstDelay < turretFireTime.ValueRO.burstDelayMax) continue;
                        foreach (WeaponBuffer weaponBuffer in weaponBuffers)
                        {
                            weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffer.weaponBuffer);
                            if(weapon.ValueRO.startFire == shouldFire) continue;
                            turretFireTime.ValueRW.burstCount++;
                            turretFireTime.ValueRW.burstCount = math.clamp(turretFireTime.ValueRO.burstCount ,0, turretFireTime.ValueRO.burstCountMax);
                            weapon.ValueRW.startFire = shouldFire;
                        }
                        turretFireTime.ValueRW.burstDelay = 0f;
                        if (turretFireTime.ValueRO.burstCount >= turretFireTime.ValueRO.burstCountMax)
                        {
                            turretFireTime.ValueRW.cooldown = turretFireTime.ValueRO.cooldownMax;
                            turretFireTime.ValueRW.burstCount = 0;
                        }
                        break;
                    case Enum.TurretFiringPattern.Individual:
                        turretFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                        if (turretFireTime.ValueRO.burstDelay < turretFireTime.ValueRO.burstDelayMax) continue;
                        weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffers[indexWeapons].weaponBuffer);
                        if (weapon.ValueRO.startFire == shouldFire) continue;
                        indexWeapons++;
                        if (indexWeapons >= weaponBuffers.Length) indexWeapons = 0;
                        turretFireTime.ValueRW.burstCount++;
                        turretFireTime.ValueRW.burstCount = math.clamp(turretFireTime.ValueRO.burstCount, 0, turretFireTime.ValueRO.burstCountMax);
                        weapon.ValueRW.startFire = shouldFire;

                        turretFireTime.ValueRW.burstDelay = 0f;
                        if (turretFireTime.ValueRO.burstCount >= turretFireTime.ValueRO.burstCountMax)
                        {
                            turretFireTime.ValueRW.cooldown = turretFireTime.ValueRO.cooldownMax;
                            turretFireTime.ValueRW.burstCount = 0;
                        }
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
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
