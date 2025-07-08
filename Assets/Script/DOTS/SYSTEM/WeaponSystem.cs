using System.Security.Cryptography;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
[UpdateAfter(typeof(TurretFireSystem))]
public partial struct WeaponSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        /*
        foreach ((RefRW<Weapon> weapon, Entity entity) in SystemAPI.Query<RefRW<Weapon>>().WithEntityAccess())
        {
            if (weapon.ValueRO.startFire)
            {
                // Giảm cooldown
                if (weapon.ValueRO.currentCooldown > 0f)
                {
                    weapon.ValueRW.currentCooldown -= SystemAPI.Time.DeltaTime;
                    continue;
                }
                else
                {
                    // có thể bắn
                    // xử lý burst
                    if (weapon.ValueRO.burstShots >= 1)
                    {
                        switch (weapon.ValueRO.firingPattern)
                        {
                            case Enum.WeaponFiringPattern.Gatling:
                                {
                                    if (weapon.ValueRO.burstCounter < weapon.ValueRO.burstShots)
                                    {
                                        // logic fire đạn (sẽ bổ sung)
                                        DynamicBuffer<BarrelAnimatorBuffer> barrelAnimatorBuffers = SystemAPI.GetBuffer<BarrelAnimatorBuffer>(entity);
                                        foreach (BarrelAnimatorBuffer barrelAnimatorBuffer in barrelAnimatorBuffers)
                                        {
                                            RefRW<BarrelAnimator> barrelAnimator = SystemAPI.GetComponentRW<BarrelAnimator>(barrelAnimatorBuffer.barrelAnimatorBuffer);
                                            if (SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity).Length > 1)
                                            {
                                                weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                                if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay + barrelAnimator.ValueRW.barrelTipIndex * barrelAnimator.ValueRW.animationDuration)
                                                {
                                                    continue;
                                                }
                                                barrelAnimator.ValueRW.barrelTipIndex++;
                                                barrelAnimator.ValueRW.pointShootIndex++;
                                                if (barrelAnimator.ValueRW.barrelTipIndex >= SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity).Length)
                                                {
                                                    barrelAnimator.ValueRW.barrelTipIndex = 0;
                                                }
                                                if (barrelAnimator.ValueRW.pointShootIndex >= SystemAPI.GetBuffer<PointShotEntityBuffer>(entity).Length)
                                                {
                                                    barrelAnimator.ValueRW.pointShootIndex = 0;
                                                }
                                                weapon.ValueRW.burstTime = 0f;
                                                weapon.ValueRW.burstCounter++;
                                                barrelAnimator.ValueRW.animationPlaying = true;
                                                barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                            }
                                            else
                                            {
                                                weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                                if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay)
                                                {
                                                    continue;
                                                }
                                                barrelAnimator.ValueRW.barrelTipIndex = 0;
                                                weapon.ValueRW.burstTime = 0f;
                                                weapon.ValueRW.burstCounter++;
                                                barrelAnimator.ValueRW.animationPlaying = true;
                                                barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // reset cooldown
                                        weapon.ValueRW.currentCooldown = weapon.ValueRO.cooldown;
                                        weapon.ValueRW.burstCounter = 0;
                                    }
                                }
                                break;
                            case Enum.WeaponFiringPattern.Individual:
                                {
                                    // logic fire đạn (sẽ bổ sung)
                                    if (weapon.ValueRO.burstCounter < weapon.ValueRO.burstShots)
                                    {
                                        // logic fire đạn (sẽ bổ sung)
                                        DynamicBuffer<BarrelAnimatorBuffer> barrelAnimatorBuffers = SystemAPI.GetBuffer<BarrelAnimatorBuffer>(entity);
                                        foreach (BarrelAnimatorBuffer barrelAnimatorBuffer in barrelAnimatorBuffers)
                                        {
                                            RefRW<BarrelAnimator> barrelAnimator = SystemAPI.GetComponentRW<BarrelAnimator>(barrelAnimatorBuffer.barrelAnimatorBuffer);
                                            if (!barrelAnimator.ValueRO.animationPlaying)
                                            {

                                                if (SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity).Length > 1)
                                                {
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                                    if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay + barrelAnimator.ValueRW.barrelTipIndex * barrelAnimator.ValueRW.animationDuration)
                                                    {
                                                        continue;
                                                    }
                                                    barrelAnimator.ValueRW.barrelTipIndex++;
                                                    barrelAnimator.ValueRW.pointShootIndex++;
                                                    if (barrelAnimator.ValueRW.barrelTipIndex >= SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity).Length)
                                                    {
                                                        barrelAnimator.ValueRW.barrelTipIndex = 0;
                                                    }
                                                    if (barrelAnimator.ValueRW.pointShootIndex >= SystemAPI.GetBuffer<PointShotEntityBuffer>(entity).Length)
                                                    {
                                                        barrelAnimator.ValueRW.pointShootIndex = 0;
                                                    }
                                                    weapon.ValueRW.burstTime = 0f;
                                                    weapon.ValueRW.burstCounter++;
                                                    barrelAnimator.ValueRW.animationPlaying = true;
                                                    barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                                }
                                                else
                                                {
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                                    if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay)
                                                    {
                                                        continue;
                                                    }
                                                    barrelAnimator.ValueRW.barrelTipIndex = 0;
                                                    weapon.ValueRW.burstTime = 0f;
                                                    weapon.ValueRW.burstCounter++;
                                                    barrelAnimator.ValueRW.animationPlaying = true;
                                                    barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // reset cooldown
                                        weapon.ValueRW.currentCooldown = weapon.ValueRO.cooldown;
                                        weapon.ValueRW.burstCounter = 0;
                                    }
                                }
                                break;
                            case Enum.WeaponFiringPattern.Simultaneous:
                                {
                                    // logic fire đạn (sẽ bổ sung)
                                }
                                break;
                            case Enum.WeaponFiringPattern.MissileLauncher:
                                {
                                    // logic fire đạn (sẽ bổ sung)
                                    if (weapon.ValueRO.burstCounter < weapon.ValueRO.burstShots)
                                    {
                                        // logic fire đạn (sẽ bổ sung)
                                        DynamicBuffer<BarrelAnimatorBuffer> barrelAnimatorBuffers = SystemAPI.GetBuffer<BarrelAnimatorBuffer>(entity);
                                        foreach (BarrelAnimatorBuffer barrelAnimatorBuffer in barrelAnimatorBuffers)
                                        {
                                            RefRW<BarrelAnimator> barrelAnimator = SystemAPI.GetComponentRW<BarrelAnimator>(barrelAnimatorBuffer.barrelAnimatorBuffer);
                                            if (!barrelAnimator.ValueRO.animationPlaying)
                                            {

                                                if (SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity).Length > 1)
                                                {
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                                    if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay + barrelAnimator.ValueRW.barrelTipIndex * barrelAnimator.ValueRW.animationDuration)
                                                    {
                                                        continue;
                                                    }
                                                    barrelAnimator.ValueRW.barrelTipIndex++;
                                                    barrelAnimator.ValueRW.pointShootIndex++;
                                                    if (barrelAnimator.ValueRW.barrelTipIndex >= SystemAPI.GetBuffer<BarrelTipEntityBuffer>(entity).Length)
                                                    {
                                                        barrelAnimator.ValueRW.barrelTipIndex = 0;
                                                    }
                                                    if (barrelAnimator.ValueRW.pointShootIndex >= SystemAPI.GetBuffer<PointShotEntityBuffer>(entity).Length)
                                                    {
                                                        barrelAnimator.ValueRW.pointShootIndex = 0;
                                                    }
                                                    weapon.ValueRW.burstTime = 0f;
                                                    weapon.ValueRW.burstCounter++;
                                                    barrelAnimator.ValueRW.animationPlaying = true;
                                                    barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                                }
                                                else
                                                {
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                                    if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay)
                                                    {
                                                        continue;
                                                    }
                                                    barrelAnimator.ValueRW.barrelTipIndex = 0;
                                                    barrelAnimator.ValueRW.pointShootIndex++;
                                                    if (barrelAnimator.ValueRW.pointShootIndex >= SystemAPI.GetBuffer<PointShotEntityBuffer>(entity).Length)
                                                    {
                                                        barrelAnimator.ValueRW.pointShootIndex = 0;
                                                    }
                                                    weapon.ValueRW.burstTime = 0f;
                                                    weapon.ValueRW.burstCounter++;
                                                    barrelAnimator.ValueRW.animationPlaying = true;
                                                    barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // reset cooldown
                                        weapon.ValueRW.currentCooldown = weapon.ValueRO.cooldown;
                                        weapon.ValueRW.burstCounter = 0;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
        */
        foreach ((RefRW<Weapon> weapon, RefRW<WeaponFireTime> weaponFireTime, RefRW<BarrelAnimator> barrelAnimator, DynamicBuffer<BarrelTipEntityBuffer> barrelTipEntityBuffers, DynamicBuffer<PointShotEntityBuffer> pointShotEntityBuffers) in SystemAPI.Query<RefRW<Weapon>, RefRW<WeaponFireTime>, RefRW<BarrelAnimator>, DynamicBuffer<BarrelTipEntityBuffer>, DynamicBuffer<PointShotEntityBuffer>>())
        {
            if (!weapon.ValueRO.startFire) continue;
            switch (weapon.ValueRO.firingPattern)
            {
                case Enum.WeaponFiringPattern.Individual:
                    if (barrelTipEntityBuffers.Length != pointShotEntityBuffers.Length) continue;
                    weaponFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                    if (weaponFireTime.ValueRO.burstDelay < weaponFireTime.ValueRO.burstDelayMax + barrelAnimator.ValueRO.animationDuration) continue;
                    barrelAnimator.ValueRW.barrelTipIndex = weaponFireTime.ValueRO.burstCount % weaponFireTime.ValueRO.burstCountMax;
                    barrelAnimator.ValueRW.pointShootIndex = weaponFireTime.ValueRO.burstCount % weaponFireTime.ValueRO.burstCountMax;
                    if (barrelAnimator.ValueRW.animationPlaying == false)
                    {
                        barrelAnimator.ValueRW.animationPlaying = true;
                        barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                    }
                    weaponFireTime.ValueRW.burstCount++;
                    weaponFireTime.ValueRW.burstCount = math.clamp(weaponFireTime.ValueRO.burstCount, 0, weaponFireTime.ValueRO.burstCountMax);
                    if(weaponFireTime.ValueRO.burstCount >= weaponFireTime.ValueRO.burstCountMax)
                    {
                        weaponFireTime.ValueRW.burstCount = 0;
                        weaponFireTime.ValueRW.burstDelay = 0;
                        weapon.ValueRW.startFire = false;
                    }
                    break;
                case Enum.WeaponFiringPattern.Simultaneous:
                    if (barrelTipEntityBuffers.Length <= 1) continue;
                    if (barrelTipEntityBuffers.Length != pointShotEntityBuffers.Length) continue;
                    weaponFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                    if (weaponFireTime.ValueRO.burstDelay < weaponFireTime.ValueRO.burstDelayMax) continue;
                    barrelAnimator.ValueRW.barrelTipIndex = weaponFireTime.ValueRO.burstCount % weaponFireTime.ValueRO.burstCountMax;
                    barrelAnimator.ValueRW.pointShootIndex = weaponFireTime.ValueRO.burstCount % weaponFireTime.ValueRO.burstCountMax;
                    if (barrelAnimator.ValueRW.animationPlaying == false)
                    {
                        barrelAnimator.ValueRW.animationPlaying = true;
                        barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                    }
                    weaponFireTime.ValueRW.burstCount += barrelTipEntityBuffers.Length;
                    weaponFireTime.ValueRW.burstCount = math.clamp(weaponFireTime.ValueRO.burstCount, 0, weaponFireTime.ValueRO.burstCountMax);
                    if (weaponFireTime.ValueRO.burstCount >= weaponFireTime.ValueRO.burstCountMax)
                    {
                        weaponFireTime.ValueRW.burstCount = 0;
                        weaponFireTime.ValueRW.burstDelay = 0;
                        weapon.ValueRW.startFire = false;
                    }
                    break;
                case Enum.WeaponFiringPattern.MissileLauncher:
                    if (barrelTipEntityBuffers.Length > 1) continue;
                    break;
                case Enum.WeaponFiringPattern.Gatling:
                    break;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
