using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(TurretFireSystem))]
public partial struct WeaponSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<WeaponFireTime, BarrelAnimation>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
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

        public void Execute(
            ref Weapon weapon,
            ref WeaponFireTime weaponFireTime,
            ref BarrelAnimation barrelAnimation,
            ref BarrelSFX barrelSFX,
            DynamicBuffer<BarrelTipEntityBuffer> barrelTipEntityBuffer)
        {
            if (!weapon.startFire) return;
            if (weapon.targetEntity == Entity.Null) return;

            switch (weapon.firingPattern)
            {
                case Enum.WeaponFiringPattern.Individual:
                    if (weaponFireTime.burstCount >= weaponFireTime.burstCountMax && !barrelAnimation.animationPlaying)
                    {
                        weaponFireTime.burstCount = 0;
                        weaponFireTime.burstDelay = 0;
                        weapon.startFire = false;
                        break;
                    }
                    weaponFireTime.burstDelay += DeltaTime;
                    if (weaponFireTime.burstDelay < weaponFireTime.burstDelayMax) break;
                    if (barrelAnimation.animationPlaying == false)
                    {
                        barrelAnimation.animationPlaying = true;
                        barrelAnimation.lastFireTime = ElapsedTime;
                        weaponFireTime.barrelTipIndex++;
                        weaponFireTime.pointShootIndex++;
                        weaponFireTime.burstCount++;
                        weaponFireTime.burstCount = math.clamp(weaponFireTime.burstCount, 0, weaponFireTime.burstCountMax);
                        if (weaponFireTime.barrelTipIndex >= barrelTipEntityBuffer.Length) weaponFireTime.barrelTipIndex = 0;
                    }
                    break;

                case Enum.WeaponFiringPattern.Simultaneous:
                    if (barrelTipEntityBuffer.Length <= 1) break;
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
                    if (barrelAnimation.animationPlaying == false)
                    {
                        barrelAnimation.animationPlaying = true;
                        barrelAnimation.lastFireTime = ElapsedTime;
                    }
                    weaponFireTime.burstCount += barrelTipEntityBuffer.Length;
                    weaponFireTime.burstCount = math.clamp(weaponFireTime.burstCount, 0, weaponFireTime.burstCountMax);
                    break;

                case Enum.WeaponFiringPattern.MissileLauncher:
                    if (barrelTipEntityBuffer.Length > 1) break;
                    if (weaponFireTime.burstCount >= weaponFireTime.burstCountMax && !barrelAnimation.animationPlaying)
                    {
                        weaponFireTime.burstCount = 0;
                        weaponFireTime.burstDelay = 0;
                        weapon.startFire = false;
                        break;
                    }
                    weaponFireTime.burstDelay += DeltaTime;
                    if (weaponFireTime.burstDelay > weaponFireTime.burstDelayMax)
                    {
                        if (barrelAnimation.animationPlaying == false)
                        {
                            barrelAnimation.animationPlaying = true;
                            barrelAnimation.lastFireTime = ElapsedTime;
                            weaponFireTime.burstDelay = 0;
                            weaponFireTime.pointShootIndex++;
                            weaponFireTime.burstCount++;
                            weaponFireTime.burstCount = math.clamp(weaponFireTime.burstCount, 0, weaponFireTime.burstCountMax);
                        }
                    }
                    break;

                case Enum.WeaponFiringPattern.Gatling:
                    // Gatling logic handled by GatlingWeaponSystem
                    break;
            }
        }
    }
}

/// <summary>
/// Separate system for Gatling weapon logic — only runs on entities with GatlingSpin component.
/// </summary>
[UpdateAfter(typeof(TurretFireSystem))]
public partial struct GatlingWeaponSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Weapon, WeaponFireTime, BarrelAnimation, GatlingSpin>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        GatlingWeaponJob job = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
        };
        job.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    public partial struct GatlingWeaponJob : IJobEntity
    {
        public float DeltaTime;
        public float ElapsedTime;

        public void Execute(
            ref Weapon weapon,
            ref WeaponFireTime weaponFireTime,
            ref BarrelAnimation barrelAnimation,
            ref BarrelSFX barrelSFX,
            ref GatlingSpin gatling)
        {
            if (!weapon.startFire) return;
            if (weapon.targetEntity == Entity.Null) return;
            if (weapon.firingPattern != Enum.WeaponFiringPattern.Gatling) return;

            if (weaponFireTime.timeOverHeat < weaponFireTime.timeOverHeatMax)
            {
                weaponFireTime.timeOverHeat += DeltaTime;
                if (!weapon.startGatling) weapon.startGatling = true;
            }
            else
            {
                if (weapon.startGatling) weapon.startGatling = false;
            }

            gatling.gatlingRotationSpeed = weapon.gatlingRotationSpeed;
            gatling.gatlingRotationSpeedChange = gatling.gatlingRotationSpeed * DeltaTime * (1f / barrelAnimation.animationDuration);

            if (weapon.startGatling)
            {
                if (gatling.currentGatlingRotation < gatling.gatlingRotationSpeed)
                {
                    gatling.currentGatlingRotation += gatling.gatlingRotationSpeedChange;
                }
            }
            else
            {
                if (gatling.currentGatlingRotation > 0)
                {
                    gatling.currentGatlingRotation -= gatling.gatlingRotationSpeedChange;
                }
            }
            gatling.currentGatlingRotation = math.clamp(gatling.currentGatlingRotation, 0f, gatling.gatlingRotationSpeed);

            if (gatling.currentGatlingRotation >= (gatling.gatlingRotationSpeed / 2))
            {
                weaponFireTime.burstDelay -= DeltaTime;
                if (weaponFireTime.burstDelay <= 0)
                {
                    Random random = barrelSFX.random;
                    if (!barrelAnimation.animationPlaying) barrelAnimation.animationPlaying = true;
                    weaponFireTime.burstDelay = weaponFireTime.burstDelayMax + random.NextFloat(-0.1f, 0.1f);
                    barrelSFX.random = random;
                }
                else
                {
                    if (barrelAnimation.animationPlaying) barrelAnimation.animationPlaying = false;
                }
            }
            else if (gatling.currentGatlingRotation > 0f && gatling.currentGatlingRotation < (gatling.gatlingRotationSpeed / 2))
            {
                if (barrelAnimation.animationPlaying) barrelAnimation.animationPlaying = false;
            }
            else
            {
                weapon.startFire = false;
                weaponFireTime.timeOverHeat = 0f;
                if (barrelAnimation.animationPlaying) barrelAnimation.animationPlaying = false;
            }
        }
    }
}
