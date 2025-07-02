using Unity.Burst;
using Unity.Entities;
[UpdateBefore(typeof(TurretFireSystem))]
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
                    continue;
                }
                else
                {
                    // có thể bắn
                    // xử lý burst
                    if (weapon.ValueRO.firingPattern == WeaponFiringPattern.Individual && weapon.ValueRO.burstShots > 1)
                    {
                        if (weapon.ValueRO.burstCounter < weapon.ValueRO.burstShots)
                        {
                            // logic fire đạn (sẽ bổ sung)
                            DynamicBuffer<BarrelAnimatorBuffer> barrelAnimatorBuffers = SystemAPI.GetBuffer<BarrelAnimatorBuffer>(entity);
                            foreach (BarrelAnimatorBuffer barrelAnimatorBuffer in barrelAnimatorBuffers)
                            {
                                RefRW<BarrelAnimator> barrelAnimator = SystemAPI.GetComponentRW<BarrelAnimator>(barrelAnimatorBuffer.barrelAnimatorBuffer);
                                if (!barrelAnimator.ValueRO.animationPlaying)
                                {
                                    weapon.ValueRW.burstTime += SystemAPI.Time.DeltaTime;
                                    if (weapon.ValueRO.burstTime <= weapon.ValueRO.burstDelay)
                                    {
                                        continue;
                                    }
                                    weapon.ValueRW.burstTime = 0f;
                                    weapon.ValueRW.burstCounter++;
                                    UnityEngine.Debug.Log($"Burst shot {weapon.ValueRO.burstCounter} for weapon {entity.Index}");
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
                            UnityEngine.Debug.Log($"Burst completed for weapon {entity.Index}");
                        }
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
