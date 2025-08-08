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
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<WeaponFireTime, BarrelAnimator, BarrelTipEntityBuffer, PointShotEntityBuffer>().Build());
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
        public void Execute(ref Weapon weapon, 
            ref WeaponFireTime weaponFireTime, 
            ref BarrelAnimator barrelAnimator, 
            DynamicBuffer<BarrelTipEntityBuffer> barrelTipEntityBuffers, 
            DynamicBuffer<PointShotEntityBuffer> pointShotEntityBuffers)
        {
            if (!weapon.startFire) return;
            if (weapon.targetEntity == Entity.Null) return;
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
