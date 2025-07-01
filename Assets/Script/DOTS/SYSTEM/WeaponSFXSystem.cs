using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
partial struct WeaponSFXSystem : ISystem
{
    private Entity entityEffect;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<Weapon> weapon, Entity entity) in SystemAPI.Query<RefRW<Weapon>>().WithEntityAccess())
        {
            if(!weapon.ValueRO.isFiring)
            {
                continue;
            }
            DynamicBuffer<BarrelAnimatorBuffer> barrelAnimatorBuffers = SystemAPI.GetBuffer<BarrelAnimatorBuffer>(entity);
            foreach(BarrelAnimatorBuffer barrelAnimatorBuffer in barrelAnimatorBuffers)
            {
                if(barrelAnimatorBuffer.barrelAnimatorBuffer == Entity.Null)
                {
                    continue;
                }
                RefRW<BarrelAnimator> barrelAnimator = SystemAPI.GetComponentRW<BarrelAnimator>(barrelAnimatorBuffer.barrelAnimatorBuffer);
                if(barrelAnimator.ValueRO.animationPlaying)
                {
                    float3 spawnEffectPosition = SystemAPI.GetComponent<LocalTransform>(barrelAnimator.ValueRO.pointShootPosition).Position;
                    if (entityEffect == Entity.Null)
                    {
                        entityEffect = state.EntityManager.Instantiate(barrelAnimator.ValueRO.muzzleFlashEntity);

                        RefRW<EffectWeaponShoot> effect = SystemAPI.GetComponentRW<EffectWeaponShoot>(entityEffect);
                        Random random = weapon.ValueRO.random;
                        float startScale = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.scaleVariance / 2f;
                        float endScale = startScale * random.NextFloat(0.6f, 0.8f);
                        float startLength = 1f + random.NextFloat(-1f, 1f) * effect.ValueRO.lengthVariance / 2f;
                        float endLength = startLength * random.NextFloat(1.75f, 3f);
                        float randomZ = random.NextFloat(-180f, 180f);
                        float pitch = math.clamp(weapon.ValueRO.sfxPitch + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);
                        float volume = math.clamp(weapon.ValueRO.sfxVolume + random.NextFloat(-1f, 1f) * 0.25f / 2f, 0.2f, 4f);

                        effect.ValueRW.startScale = startScale;
                        effect.ValueRW.endScale = endScale;
                        effect.ValueRW.startLength = startLength;
                        effect.ValueRW.endLength = endLength;
                        effect.ValueRW.isInitialized = true;
                        effect.ValueRW.sfxPitch = pitch;
                        effect.ValueRW.sfxVolume = volume;

                        RefRW<LocalTransform> localTranform = SystemAPI.GetComponentRW<LocalTransform>(effect.ValueRO.muzzleFlashEffect);
                        localTranform.ValueRW.Position = spawnEffectPosition;
                        localTranform.ValueRW.Rotation = quaternion.EulerXYZ(0f, 0f, math.radians(randomZ));
                        RefRW<PostTransformMatrix> visualEffectPostTransformMatrix = SystemAPI.GetComponentRW<PostTransformMatrix>(effect.ValueRO.muzzleFlashEffect);
                        visualEffectPostTransformMatrix.ValueRW.Value = float4x4.Scale(startScale, startScale, startLength);
                        weapon.ValueRW.random = random;
                    }
                    else
                    {
                        continue;
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
