using FMOD.Studio;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(BarrelFireEffectSystem))]
partial struct PlaySoundFireEffectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<SoundWeaponEffectShoot> soundWeaponEffectShoot, RefRO<LocalToWorld> localToWorld, Entity entity) in SystemAPI.Query<RefRW<SoundWeaponEffectShoot>, RefRO<LocalToWorld>>().WithEntityAccess())
        {
            if (soundWeaponEffectShoot.ValueRO.isPlayOneShot)
            {
                FmodSoundManager.PlayOneShotFireSound(
                    soundWeaponEffectShoot.ValueRO.soundEventReferenceSoundWeaponEffectShootGUID,
                    soundWeaponEffectShoot.ValueRO.pitch,
                    soundWeaponEffectShoot.ValueRO.volume,
                    localToWorld.ValueRO.Position);
                soundWeaponEffectShoot.ValueRW.isPlayOneShot = false;
            }
        }
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
