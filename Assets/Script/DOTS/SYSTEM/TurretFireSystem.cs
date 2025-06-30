using Unity.Burst;
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
        foreach ((RefRO<Turret> turret, RefRO<LocalTransform> localTransform, Entity entity) in SystemAPI.Query<RefRO<Turret>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            bool shouldFire = false;
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
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
