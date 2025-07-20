using System.Security.Cryptography;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using static UnityEngine.EventSystems.EventTrigger;
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
                    weapon.ValueRW.currentCooldown -= SystemAPI.Time.deltaTime;
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
                                                weapon.ValueRW.burstTime += SystemAPI.Time.deltaTime;
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
                                                weapon.ValueRW.burstTime += SystemAPI.Time.deltaTime;
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
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.deltaTime;
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
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.deltaTime;
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
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.deltaTime;
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
                                                    weapon.ValueRW.burstTime += SystemAPI.Time.deltaTime;
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
        /*
        foreach ((RefRW<Weapon> weapon, RefRW<WeaponFireTime> weaponFireTime, RefRW<BarrelAnimator> barrelAnimator, DynamicBuffer<BarrelTipEntityBuffer> barrelTipEntityBuffers, DynamicBuffer<PointShotEntityBuffer> pointShotEntityBuffers) 
            in 
            SystemAPI.Query<RefRW<Weapon>, RefRW<WeaponFireTime>, RefRW<BarrelAnimator>, DynamicBuffer<BarrelTipEntityBuffer>, DynamicBuffer<PointShotEntityBuffer>>())
        {
            if (!weapon.ValueRO.startFire) continue;
            switch (weapon.ValueRO.firingPattern)
            {
                case Enum.WeaponFiringPattern.Individual:
                    if (weaponFireTime.ValueRO.burstCount >= weaponFireTime.ValueRO.burstCountMax && !barrelAnimator.ValueRO.animationPlaying)
                    {
                        weaponFireTime.ValueRW.burstCount = 0;
                        weaponFireTime.ValueRW.burstDelay = 0;
                        weapon.ValueRW.startFire = false;
                        break;
                    }
                    if (barrelTipEntityBuffers.Length != pointShotEntityBuffers.Length) break;
                    weaponFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                    if (weaponFireTime.ValueRO.burstDelay < weaponFireTime.ValueRO.burstDelayMax) break;
                    if (barrelAnimator.ValueRW.animationPlaying == false)
                    {
                        barrelAnimator.ValueRW.animationPlaying = true;
                        barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                        weaponFireTime.ValueRW.barrelTipIndex++;
                        weaponFireTime.ValueRW.pointShootIndex++;
                        weaponFireTime.ValueRW.burstCount++;
                        weaponFireTime.ValueRW.burstCount = math.clamp(weaponFireTime.ValueRO.burstCount, 0, weaponFireTime.ValueRO.burstCountMax);
                        if (weaponFireTime.ValueRO.barrelTipIndex >= barrelTipEntityBuffers.Length) weaponFireTime.ValueRW.barrelTipIndex = 0;
                        if (weaponFireTime.ValueRO.pointShootIndex >= pointShotEntityBuffers.Length) weaponFireTime.ValueRW.pointShootIndex = 0;
                    }
                    break;
                case Enum.WeaponFiringPattern.Simultaneous:
                    if (barrelTipEntityBuffers.Length <= 1) break;
                    if (barrelTipEntityBuffers.Length != pointShotEntityBuffers.Length) break;
                    if (weaponFireTime.ValueRO.burstCount >= weaponFireTime.ValueRO.burstCountMax)
                    {
                        weaponFireTime.ValueRW.burstCount = 0;
                        weaponFireTime.ValueRW.burstDelay = 0;
                        weapon.ValueRW.startFire = false;
                        break;
                    }
                    weaponFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                    if (weaponFireTime.ValueRO.burstDelay < weaponFireTime.ValueRO.burstDelayMax) break;
                    weaponFireTime.ValueRW.barrelTipIndex = weaponFireTime.ValueRO.burstCount % weaponFireTime.ValueRO.burstCountMax;
                    weaponFireTime.ValueRW.pointShootIndex = weaponFireTime.ValueRO.burstCount % weaponFireTime.ValueRO.burstCountMax;
                    if (barrelAnimator.ValueRW.animationPlaying == false)
                    {
                        barrelAnimator.ValueRW.animationPlaying = true;
                        barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                    }
                    weaponFireTime.ValueRW.burstCount += barrelTipEntityBuffers.Length;
                    weaponFireTime.ValueRW.burstCount = math.clamp(weaponFireTime.ValueRO.burstCount, 0, weaponFireTime.ValueRO.burstCountMax);

                    weaponFireTime.ValueRW.burstCount += barrelTipEntityBuffers.Length;
                    break;
                case Enum.WeaponFiringPattern.MissileLauncher:
                    if (barrelTipEntityBuffers.Length > 1) break;
                    if (weaponFireTime.ValueRO.burstCount >= weaponFireTime.ValueRO.burstCountMax && !barrelAnimator.ValueRO.animationPlaying)
                    {
                        weaponFireTime.ValueRW.burstCount = 0;
                        weaponFireTime.ValueRW.burstDelay = 0;
                        weapon.ValueRW.startFire = false;
                        break;
                    }
                    weaponFireTime.ValueRW.burstDelay += SystemAPI.Time.DeltaTime;
                    if (weaponFireTime.ValueRO.burstDelay > weaponFireTime.ValueRO.burstDelayMax)
                    {
                        if (barrelAnimator.ValueRW.animationPlaying == false)
                        {
                            barrelAnimator.ValueRW.animationPlaying = true;
                            barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                            weaponFireTime.ValueRW.burstDelay = 0;
                            weaponFireTime.ValueRW.pointShootIndex++;
                            weaponFireTime.ValueRW.burstCount++;
                            weaponFireTime.ValueRW.burstCount = math.clamp(weaponFireTime.ValueRO.burstCount, 0, weaponFireTime.ValueRO.burstCountMax);
                            if (weaponFireTime.ValueRO.pointShootIndex >= pointShotEntityBuffers.Length) weaponFireTime.ValueRW.pointShootIndex = 0;
                        }
                    }
                    break;
                case Enum.WeaponFiringPattern.Gatling:
                    if (weaponFireTime.ValueRO.timeOverHeat < weaponFireTime.ValueRO.timeOverHeatMax)
                    {
                        weaponFireTime.ValueRW.timeOverHeat += SystemAPI.Time.DeltaTime;
                        if(!weapon.ValueRO.startGatling) weapon.ValueRW.startGatling = true;
                    }
                    else
                    {
                        if (weapon.ValueRO.startGatling) weapon.ValueRW.startGatling = false;
                    }
                    barrelAnimator.ValueRW.gatlingRotationSpeed = weapon.ValueRO.gatlingRotationSpeed;
                    barrelAnimator.ValueRW.gatlingRotationSpeedChange = barrelAnimator.ValueRO.gatlingRotationSpeed * SystemAPI.Time.DeltaTime * (1f / barrelAnimator.ValueRO.animationDuration);
                    if (weapon.ValueRO.startGatling)
                    {
                        if (barrelAnimator.ValueRO.curentGatlingRotation < barrelAnimator.ValueRO.gatlingRotationSpeed)
                        {
                            barrelAnimator.ValueRW.curentGatlingRotation += barrelAnimator.ValueRO.gatlingRotationSpeedChange;
                        }
                    }
                    else
                    {
                        if (barrelAnimator.ValueRO.curentGatlingRotation > 0)
                        {
                            barrelAnimator.ValueRW.curentGatlingRotation -= barrelAnimator.ValueRO.gatlingRotationSpeedChange;
                        }
                    }
                    barrelAnimator.ValueRW.curentGatlingRotation = math.clamp(barrelAnimator.ValueRO.curentGatlingRotation, 0f, barrelAnimator.ValueRO.gatlingRotationSpeed);
                    if (barrelAnimator.ValueRO.curentGatlingRotation >= (barrelAnimator.ValueRO.gatlingRotationSpeed / 2))
                    {
                        weaponFireTime.ValueRW.burstDelay -= SystemAPI.Time.DeltaTime;
                        if (weaponFireTime.ValueRO.burstDelay <= 0)
                        {
                            Random random = barrelAnimator.ValueRO.random;
                            if (!barrelAnimator.ValueRO.animationPlaying) barrelAnimator.ValueRW.animationPlaying = true;
                            weaponFireTime.ValueRW.burstDelay = weaponFireTime.ValueRO.burstDelayMax + random.NextFloat(-0.1f, 0.1f);
                            barrelAnimator.ValueRW.random = random;
                        }
                        else
                        {
                            if (barrelAnimator.ValueRO.animationPlaying) barrelAnimator.ValueRW.animationPlaying = false;
                        }
                    }
                    else if (barrelAnimator.ValueRO.curentGatlingRotation > 0f && barrelAnimator.ValueRO.curentGatlingRotation < (barrelAnimator.ValueRO.gatlingRotationSpeed / 2))
                    {
                        if (barrelAnimator.ValueRO.animationPlaying) barrelAnimator.ValueRW.animationPlaying = false;
                    }
                    else
                    {
                        weapon.ValueRW.startFire = false;
                        weaponFireTime.ValueRW.timeOverHeat = 0f;
                        if (barrelAnimator.ValueRO.animationPlaying) barrelAnimator.ValueRW.animationPlaying = false;
                    }
                    break;
            }
        }
        */
        WeaponSystemJob weaponSystemJob = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
        };
        weaponSystemJob.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile] 
    public partial struct WeaponSystemJob : IJobEntity
    {
        public float DeltaTime;
        public float ElapsedTime;
        public void Execute(ref Weapon weapon, 
            ref WeaponFireTime weaponFireTime, 
            ref BarrelAnimator barrelAnimator, 
            DynamicBuffer<BarrelTipEntityBuffer> barrelTipEntityBuffers, 
            DynamicBuffer<PointShotEntityBuffer> pointShotEntityBuffers)
        {
            if (!weapon.startFire) return;
            switch (weapon.firingPattern)
            {
                case Enum.WeaponFiringPattern.Individual:
                    if (weaponFireTime.burstCount >= weaponFireTime.burstCountMax && !barrelAnimator.animationPlaying)
                    {
                        weaponFireTime.burstCount = 0;
                        weaponFireTime.burstDelay = 0;
                        weapon.startFire = false;
                        break;
                    }
                    if (barrelTipEntityBuffers.Length != pointShotEntityBuffers.Length) break;
                    weaponFireTime.burstDelay += DeltaTime;
                    if (weaponFireTime.burstDelay < weaponFireTime.burstDelayMax) break;
                    if (barrelAnimator.animationPlaying == false)
                    {
                        barrelAnimator.animationPlaying = true;
                        barrelAnimator.lastFireTime = ElapsedTime;
                        weaponFireTime.barrelTipIndex++;
                        weaponFireTime.pointShootIndex++;
                        weaponFireTime.burstCount++;
                        weaponFireTime.burstCount = math.clamp(weaponFireTime.burstCount, 0, weaponFireTime.burstCountMax);
                        if (weaponFireTime.barrelTipIndex >= barrelTipEntityBuffers.Length) weaponFireTime.barrelTipIndex = 0;
                        if (weaponFireTime.pointShootIndex >= pointShotEntityBuffers.Length) weaponFireTime.pointShootIndex = 0;
                    }
                    break;
                case Enum.WeaponFiringPattern.Simultaneous:
                    if (barrelTipEntityBuffers.Length <= 1) break;
                    if (barrelTipEntityBuffers.Length != pointShotEntityBuffers.Length) break;
                    if (weaponFireTime.burstCount >= weaponFireTime.burstCountMax)
                    {
                        weaponFireTime.burstCount = 0;
                        weaponFireTime.burstDelay = 0;
                        weapon.startFire = false;
                        break;
                    }
                    weaponFireTime.burstDelay += DeltaTime;
                    if (weaponFireTime.burstDelay < weaponFireTime.burstDelayMax) break;
                    weaponFireTime.barrelTipIndex = weaponFireTime.burstCount % weaponFireTime.burstCountMax;
                    weaponFireTime.pointShootIndex = weaponFireTime.burstCount % weaponFireTime.burstCountMax;
                    if (barrelAnimator.animationPlaying == false)
                    {
                        barrelAnimator.animationPlaying = true;
                        barrelAnimator.lastFireTime = ElapsedTime;
                    }
                    weaponFireTime.burstCount += barrelTipEntityBuffers.Length;
                    weaponFireTime.burstCount = math.clamp(weaponFireTime.burstCount, 0, weaponFireTime.burstCountMax);

                    weaponFireTime.burstCount += barrelTipEntityBuffers.Length;
                    break;
                case Enum.WeaponFiringPattern.MissileLauncher:
                    if (barrelTipEntityBuffers.Length > 1) break;
                    if (weaponFireTime.burstCount >= weaponFireTime.burstCountMax && !barrelAnimator.animationPlaying)
                    {
                        weaponFireTime.burstCount = 0;
                        weaponFireTime.burstDelay = 0;
                        weapon.startFire = false;
                        break;
                    }
                    weaponFireTime.burstDelay += DeltaTime;
                    if (weaponFireTime.burstDelay > weaponFireTime.burstDelayMax)
                    {
                        if (barrelAnimator.animationPlaying == false)
                        {
                            barrelAnimator.animationPlaying = true;
                            barrelAnimator.lastFireTime = ElapsedTime;
                            weaponFireTime.burstDelay = 0;
                            weaponFireTime.pointShootIndex++;
                            weaponFireTime.burstCount++;
                            weaponFireTime.burstCount = math.clamp(weaponFireTime.burstCount, 0, weaponFireTime.burstCountMax);
                            if (weaponFireTime.pointShootIndex >= pointShotEntityBuffers.Length) weaponFireTime.pointShootIndex = 0;
                        }
                    }
                    break;
                case Enum.WeaponFiringPattern.Gatling:
                    if (weaponFireTime.timeOverHeat < weaponFireTime.timeOverHeatMax)
                    {
                        weaponFireTime.timeOverHeat += DeltaTime;
                        if (!weapon.startGatling) weapon.startGatling = true;
                    }
                    else
                    {
                        if (weapon.startGatling) weapon.startGatling = false;
                    }
                    barrelAnimator.gatlingRotationSpeed = weapon.gatlingRotationSpeed;
                    barrelAnimator.gatlingRotationSpeedChange = barrelAnimator.gatlingRotationSpeed * DeltaTime * (1f / barrelAnimator.animationDuration);
                    if (weapon.startGatling)
                    {
                        if (barrelAnimator.curentGatlingRotation < barrelAnimator.gatlingRotationSpeed)
                        {
                            barrelAnimator.curentGatlingRotation += barrelAnimator.gatlingRotationSpeedChange;
                        }
                    }
                    else
                    {
                        if (barrelAnimator.curentGatlingRotation > 0)
                        {
                            barrelAnimator.curentGatlingRotation -= barrelAnimator.gatlingRotationSpeedChange;
                        }
                    }
                    barrelAnimator.curentGatlingRotation = math.clamp(barrelAnimator.curentGatlingRotation, 0f,     barrelAnimator.gatlingRotationSpeed);
                    if (barrelAnimator.curentGatlingRotation >= (barrelAnimator.gatlingRotationSpeed / 2))
                    {
                        weaponFireTime.burstDelay -= DeltaTime;
                        if (weaponFireTime.burstDelay <= 0)
                        {
                            Random random = barrelAnimator.random;
                            if (!barrelAnimator.animationPlaying) barrelAnimator.animationPlaying = true;
                            weaponFireTime.burstDelay = weaponFireTime.burstDelayMax + random.NextFloat(-0.1f, 0.1f);
                            barrelAnimator.random = random;
                        }
                        else
                        {
                            if (barrelAnimator.animationPlaying) barrelAnimator.animationPlaying = false;
                        }
                    }
                    else if (barrelAnimator.curentGatlingRotation > 0f && barrelAnimator.curentGatlingRotation < (barrelAnimator.gatlingRotationSpeed / 2))
                    {
                        if (barrelAnimator.animationPlaying) barrelAnimator.animationPlaying = false;
                    }
                    else
                    {
                        weapon.startFire = false;
                        weaponFireTime.timeOverHeat = 0f;
                        if (barrelAnimator.animationPlaying) barrelAnimator.animationPlaying = false;
                    }
                    break;
            }
        }
    }
}
