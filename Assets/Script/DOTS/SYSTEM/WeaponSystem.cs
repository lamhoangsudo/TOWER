using Unity.Burst;
using Unity.Entities;

public partial struct WeaponSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<Weapon> weapon, Entity entity) in SystemAPI.Query<RefRW<Weapon>>().WithEntityAccess())
        {
            if (weapon.ValueRO.isFiring)
            {
                // Giảm cooldown
                if (weapon.ValueRO.currentCooldown > 0f)
                {
                    weapon.ValueRW.currentCooldown -= SystemAPI.Time.DeltaTime;
                }

                if (weapon.ValueRO.currentCooldown <= 0f)
                {
                    // có thể bắn
                    // reset cooldown
                    weapon.ValueRW.currentCooldown = weapon.ValueRO.cooldown;

                    // xử lý burst
                    if (weapon.ValueRO.firingPattern == WeaponFiringPattern.Individual && weapon.ValueRO.burstShots > 1)
                    {
                        if (weapon.ValueRO.burstCounter > 0)
                        {
                            weapon.ValueRW.burstCounter--;
                            float bustTime = 0;
                            // logic fire đạn (sẽ bổ sung)
                            DynamicBuffer<BarrelAnimatorBuffer> barrelAnimatorBuffers = SystemAPI.GetBuffer<BarrelAnimatorBuffer>(entity);
                            foreach(BarrelAnimatorBuffer barrelAnimatorBuffer in barrelAnimatorBuffers)
                            {
                                bustTime -= SystemAPI.Time.DeltaTime;
                                if (bustTime > 0f)
                                {
                                    continue;
                                }
                                RefRW<BarrelAnimator> barrelAnimator = SystemAPI.GetComponentRW<BarrelAnimator>(barrelAnimatorBuffer.barrelAnimatorBuffer);
                                barrelAnimator.ValueRW.animationPlaying = true;
                                barrelAnimator.ValueRW.lastFireTime = (float)SystemAPI.Time.ElapsedTime;
                                bustTime = weapon.ValueRO.burstDelay;
                            }
                        }
                        else
                        {
                            // reset burstCounter
                            weapon.ValueRW.burstCounter = weapon.ValueRO.burstShots - 1;
                        }
                    }
                    else
                    {
                        // logic fire đạn (sẽ bổ sung)
                    }
                }
            }

        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
