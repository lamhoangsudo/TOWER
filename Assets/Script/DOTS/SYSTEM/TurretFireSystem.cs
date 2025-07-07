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
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<Turret> turret, RefRO<LocalTransform> localTransform, Entity entity) in SystemAPI.Query<RefRO<Turret>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (turret.ValueRO.autoFire && turret.ValueRO.isHeadingRotationTarget && turret.ValueRO.isElevationRotationTarget)
            {
                shouldFire = true;
            }
            else
            {
                shouldFire = false;
            }
            DynamicBuffer<WeaponItemBuffer> weaponBuffers = SystemAPI.GetBuffer<WeaponItemBuffer>(entity);
            foreach (WeaponItemBuffer weaponItem in weaponBuffers)
            {
                RefRW<Weapon> weapon = SystemAPI.GetComponentRW<Weapon>(weaponItem.weaponEntity);
                if (weapon.ValueRO.isFiring != shouldFire)
                {
                    weapon.ValueRW.isFiring = shouldFire;
                }
            }
        }
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
                            if(weapon.ValueRO.isFiring != shouldFire) continue;
                            turretFireTime.ValueRW.burstCount++;
                            turretFireTime.ValueRW.burstCount = math.clamp(turretFireTime.ValueRO.burstCount ,0f, turretFireTime.ValueRO.burstCountMax);
                            weapon.ValueRW.isFiring = shouldFire;
                        }
                        turretFireTime.ValueRW.burstDelay = 0f;
                        if (turretFireTime.ValueRO.burstCount >= turretFireTime.ValueRO.burstCountMax)
                        {
                            turretFireTime.ValueRW.cooldown = turretFireTime.ValueRO.cooldownMax;
                            turretFireTime.ValueRW.burstCount = 0f;
                        }
                        break;
                    case Enum.TurretFiringPattern.Individual:
                        turretFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                        if (turretFireTime.ValueRO.burstDelay < turretFireTime.ValueRO.burstDelayMax) continue;

                        int indexWeapons = (int)(turretFireTime.ValueRO.burstCount / turretFireTime.ValueRO.burstCountMax);
                        weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffers[indexWeapons].weaponBuffer);
                        if (weapon.ValueRO.isFiring != shouldFire) continue;
                        turretFireTime.ValueRW.burstCount++;
                        turretFireTime.ValueRW.burstCount = math.clamp(turretFireTime.ValueRO.burstCount, 0f, turretFireTime.ValueRO.burstCountMax);
                        weapon.ValueRW.isFiring = shouldFire;

                        turretFireTime.ValueRW.burstDelay = 0f;
                        if (turretFireTime.ValueRO.burstCount >= turretFireTime.ValueRO.burstCountMax)
                        {
                            turretFireTime.ValueRW.cooldown = turretFireTime.ValueRO.cooldownMax;
                            turretFireTime.ValueRW.burstCount = 0f;
                        }
                        break;
                }
            }
            else if (weaponBuffers.Length == 1)
            {
                weapon = SystemAPI.GetComponentRW<Weapon>(weaponBuffers[0].weaponBuffer);
                if (weapon.ValueRO.isFiring != shouldFire)
                {
                    weapon.ValueRW.isFiring = shouldFire;
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
