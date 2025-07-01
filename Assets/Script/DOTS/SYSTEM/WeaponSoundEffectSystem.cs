using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
[UpdateAfter(typeof(WeaponSFXSystem))]
partial struct WeaponSoundEffectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRO<EffectWeaponShoot> effectWeaponShoot, Entity entity) in SystemAPI.Query<RefRO<EffectWeaponShoot>>().WithEntityAccess())
        {
            if(!effectWeaponShoot.ValueRO.isInitialized)
            {
                continue;
            }
            AudioSource audioSource = state.World.EntityManager.GetComponentObject<AudioSource>(entity);
            audioSource.pitch = effectWeaponShoot.ValueRO.sfxPitch;
            audioSource.volume = effectWeaponShoot.ValueRO.sfxVolume;
            //audioSource.Play();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
