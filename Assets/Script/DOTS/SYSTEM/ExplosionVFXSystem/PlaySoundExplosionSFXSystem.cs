using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

public partial struct PlaySoundExplosionSFXSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<ExplosionSFX> explosionSFX, RefRO<LocalToWorld> localToWorld) in  SystemAPI.Query<RefRW<ExplosionSFX>, RefRO<LocalToWorld>>())
        {
            if(explosionSFX.ValueRO.isPlayShoot)
            {
                FmodSoundManager.PlayOneShotFireSound(
                    explosionSFX.ValueRO.soundEventReferenceExplosionSFXGUID,
                    explosionSFX.ValueRO.pitch,
                    explosionSFX.ValueRO.volume,
                    localToWorld.ValueRO.Position);
                explosionSFX.ValueRW.isPlayShoot = false;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
