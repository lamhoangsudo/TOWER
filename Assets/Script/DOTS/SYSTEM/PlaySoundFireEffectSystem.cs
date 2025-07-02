using Unity.Burst;
using Unity.Entities;
using UnityEngine;
[UpdateAfter(typeof(BarrelAnimatorSystem))]
partial struct PlaySoundFireEffectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot, Entity entity) in SystemAPI.Query<RefRW<SoundWeaponEffectShoot>>().WithEntityAccess())
        {
            if (soundWeaponEffectShoot.ValueRO.isPlayOneShot)
            {
                AudioSource audioSource = state.World.EntityManager.GetComponentObject<AudioSource>(entity);
                audioSource.pitch = soundWeaponEffectShoot.ValueRO.pitch;
                audioSource.volume = soundWeaponEffectShoot.ValueRO.volume;
                audioSource.PlayOneShot(audioSource.clip);
                soundWeaponEffectShoot.ValueRW.isPlayOneShot = false;
            }
        }
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
