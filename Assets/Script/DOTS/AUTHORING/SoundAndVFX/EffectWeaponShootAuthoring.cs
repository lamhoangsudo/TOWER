using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
[UpdateAfter(typeof(BarrelFireEffectSystem))]
public class EffectWeaponShootAuthoring : MonoBehaviour
{
    public Light lightEffect;
    public VisualEffect visualEffect;
    public float muzzleFlashDuration;
    public float lightIntensity;
    public class EffectWeaponShootAuthoringBaker : Baker<EffectWeaponShootAuthoring>
    {
        public override void Bake(EffectWeaponShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EffectWeaponShoot
            {
                muzzleFlashDuration = authoring.muzzleFlashDuration,
                lightEffect = GetEntity(authoring.lightEffect, TransformUsageFlags.Dynamic),
                visualEffect = GetEntity(authoring.visualEffect, TransformUsageFlags.Dynamic),
                elapsedTime = authoring.muzzleFlashDuration,
                sfxPitch = 0f,
                sfxVolume = 0f,
                isPlayOneShot = false,
                lightIntensity = authoring.lightEffect != null ? authoring.lightIntensity : 0f,
            });
        }
    }
}

public struct EffectWeaponShoot : IComponentData
{
    public Entity lightEffect;
    public Entity visualEffect;
    public float muzzleFlashDuration;
    public float elapsedTime;
    public float sfxPitch;
    public float sfxVolume;
    public bool isPlayOneShot;
    public float lightIntensity;
    public float3 SpawnPosition;
    public quaternion SpawnRandomRotation;
}


