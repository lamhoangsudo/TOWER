using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
[UpdateAfter(typeof(BarrelAnimatorSystem))]
public class EffectWeaponShootAuthoring : MonoBehaviour
{
    public Light lightEffect;
    public GameObject muzzleFlashEffect;
    public float scaleVariance;
    public float lengthVariance;
    public float muzzleFlashDuration;
    public float elapsedTime;
    public float startScale;
    public float endScale;
    public float startLength;
    public float endLength;
    public float lightIntensity;
    public class EffectWeaponShootAuthoringBaker : Baker<EffectWeaponShootAuthoring>
    {
        public override void Bake(EffectWeaponShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EffectWeaponShoot
            {
                lengthVariance = authoring.lengthVariance,
                scaleVariance = authoring.scaleVariance,
                muzzleFlashDuration = authoring.muzzleFlashDuration,
                lightEffect = GetEntity(authoring.lightEffect, TransformUsageFlags.Dynamic),
                muzzleFlashEffect = GetEntity(authoring.muzzleFlashEffect, TransformUsageFlags.NonUniformScale),
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
    public Entity muzzleFlashEffect;
    public float scaleVariance;
    public float lengthVariance;
    public float muzzleFlashDuration;
    public float elapsedTime;
    public float startScale;
    public float endScale;
    public float startLength;
    public float endLength;
    public float sfxPitch;
    public float sfxVolume;
    public bool isPlayOneShot;
    public float lightIntensity;
    public float3 SpawnPosition;
    public quaternion SpawnRandomRotation;
}


