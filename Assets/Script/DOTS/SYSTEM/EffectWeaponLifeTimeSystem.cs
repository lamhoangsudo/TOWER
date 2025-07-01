using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateBefore(typeof(BarrelAnimatorSystem))]
partial struct EffectWeaponLifeTimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach ((RefRW<EffectWeaponShoot> effectWeaponShoot, Entity entity) in SystemAPI.Query<RefRW<EffectWeaponShoot>>().WithEntityAccess())
        {
            effectWeaponShoot.ValueRW.elapsedTime -= SystemAPI.Time.DeltaTime;
            if (effectWeaponShoot.ValueRO.elapsedTime <= 0)
            {
                effectWeaponShoot.ValueRW.isInitialized = false;
                ecb.DestroyEntity(entity);
                return;
            }
            RefRW<PostTransformMatrix> visualEffectPostTransformMatrix = SystemAPI.GetComponentRW<PostTransformMatrix>(effectWeaponShoot.ValueRO.muzzleFlashEffect);
            visualEffectPostTransformMatrix.ValueRW.Value = float4x4.Scale(
                Mathf.Lerp(effectWeaponShoot.ValueRO.startScale, effectWeaponShoot.ValueRO.endScale, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration), 
                Mathf.Lerp(effectWeaponShoot.ValueRO.startScale, effectWeaponShoot.ValueRO.endScale, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration), 
                Mathf.Lerp(effectWeaponShoot.ValueRO.startLength, effectWeaponShoot.ValueRO.endLength, effectWeaponShoot.ValueRO.elapsedTime / effectWeaponShoot.ValueRO.muzzleFlashDuration)
                );
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
