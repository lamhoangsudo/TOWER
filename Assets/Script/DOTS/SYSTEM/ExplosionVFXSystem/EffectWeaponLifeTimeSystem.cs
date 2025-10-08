using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;
[UpdateAfter(typeof(BarrelAnimatorSystem))]
public partial struct EffectWeaponLifeTimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<EffectWeaponShoot> effectWeaponShoot, RefRW<LocalTransform> localTransform, Entity entity) in SystemAPI.Query<RefRW<EffectWeaponShoot>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (!effectWeaponShoot.ValueRO.isPlayOneShot) continue;
            localTransform.ValueRW.Scale = 1f;
            if (effectWeaponShoot.ValueRO.elapsedTime <= 0)
            {
                effectWeaponShoot.ValueRW.elapsedTime = effectWeaponShoot.ValueRO.muzzleFlashDuration;
                localTransform.ValueRW.Scale = 0f;
                effectWeaponShoot.ValueRW.isPlayOneShot = false;
                if (effectWeaponShoot.ValueRO.lightEffect != Entity.Null)
                {
                    Light light = state.EntityManager.GetComponentObject<Light>(effectWeaponShoot.ValueRO.lightEffect);
                    light.intensity = 0f;
                }
                continue;
            }
            else
            {
                if (!localTransform.ValueRO.Position.Equals(effectWeaponShoot.ValueRO.SpawnPosition))
                {
                    localTransform.ValueRW.Position = effectWeaponShoot.ValueRO.SpawnPosition;
                }
                if (!localTransform.ValueRO.Rotation.Equals(effectWeaponShoot.ValueRO.SpawnRandomRotation))
                {
                    localTransform.ValueRW.Rotation = effectWeaponShoot.ValueRO.SpawnRandomRotation;
                }
                effectWeaponShoot.ValueRW.elapsedTime -= SystemAPI.Time.DeltaTime;
                if (effectWeaponShoot.ValueRO.lightEffect != Entity.Null)
                {
                    Light light = state.EntityManager.GetComponentObject<Light>(effectWeaponShoot.ValueRO.lightEffect);
                    light.intensity = Mathf.Lerp(effectWeaponShoot.ValueRO.lightIntensity, 0f, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration);
                }
                VisualEffect vfx = state.EntityManager.GetComponentObject<VisualEffect>(effectWeaponShoot.ValueRO.visualEffect);
                vfx.Play();
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
