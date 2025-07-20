using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(BarrelAnimatorSystem))]
partial struct EffectWeaponLifeTimeSystem : ISystem
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
            RefRW<PostTransformMatrix> postTransformMatrix = SystemAPI.GetComponentRW<PostTransformMatrix>(effectWeaponShoot.ValueRO.muzzleFlashEffect);
            if (effectWeaponShoot.ValueRO.elapsedTime <= 0)
            {
                effectWeaponShoot.ValueRW.elapsedTime = effectWeaponShoot.ValueRO.muzzleFlashDuration;
                localTransform.ValueRW.Scale = 0f;
                effectWeaponShoot.ValueRW.isPlayOneShot = false;
                effectWeaponShoot.ValueRW.endScale = 0;
                effectWeaponShoot.ValueRW.endLength = 0;
                effectWeaponShoot.ValueRW.startScale = 0;
                effectWeaponShoot.ValueRW.startLength = 0;
                if (effectWeaponShoot.ValueRO.lightEffect != Entity.Null)
                {
                    Light light = state.EntityManager.GetComponentObject<Light>(effectWeaponShoot.ValueRO.lightEffect);
                    light.intensity = 0f;
                }
                continue;
            }
            else
            {
                if (postTransformMatrix.ValueRO.Value.Equals(float4x4.Scale(0f, 0f, 0f)))
                {
                    postTransformMatrix.ValueRW.Value = float4x4.Scale(effectWeaponShoot.ValueRO.startScale, effectWeaponShoot.ValueRO.startScale, effectWeaponShoot.ValueRO.startLength);
                }
                if (!localTransform.ValueRO.Position.Equals(effectWeaponShoot.ValueRO.SpawnPosition))
                {
                    localTransform.ValueRW.Position = effectWeaponShoot.ValueRO.SpawnPosition;
                }
                if(!localTransform.ValueRO.Rotation.Equals(effectWeaponShoot.ValueRO.SpawnRandomRotation))
                {
                    localTransform.ValueRW.Rotation = effectWeaponShoot.ValueRO.SpawnRandomRotation;
                }
                effectWeaponShoot.ValueRW.elapsedTime -= SystemAPI.Time.DeltaTime;
                if (effectWeaponShoot.ValueRO.lightEffect != Entity.Null)
                {
                    Light light = state.EntityManager.GetComponentObject<Light>(effectWeaponShoot.ValueRO.lightEffect);
                    light.intensity = Mathf.Lerp(effectWeaponShoot.ValueRO.lightIntensity, 0f, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration);
                }
                postTransformMatrix.ValueRW.Value = float4x4.Scale(
                    Mathf.Lerp(effectWeaponShoot.ValueRO.startScale, effectWeaponShoot.ValueRO.endScale, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration),
                    Mathf.Lerp(effectWeaponShoot.ValueRO.startScale, effectWeaponShoot.ValueRO.endScale, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration),
                    Mathf.Lerp(effectWeaponShoot.ValueRO.startLength, effectWeaponShoot.ValueRO.endLength, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration)
                    );
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
