using Unity.Entities;
using UnityEngine;

public class EffectWeaponShootAuthoring : MonoBehaviour
{
    public AudioSource audioSource;
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
                muzzleFlashEffect = GetEntity(authoring.muzzleFlashEffect, TransformUsageFlags.NonUniformScale),
                isInitialized = false,
                elapsedTime = authoring.muzzleFlashDuration,
                sfxPitch = 0f,
                sfxVolume = 0f,
            });
            AddComponentObject<AudioSource>(entity, authoring.audioSource);
            AddComponentObject<Light>(entity, authoring.lightEffect);
        }
    }
}

public struct EffectWeaponShoot : IComponentData
{
    public Entity muzzleFlashEffect;
    public float scaleVariance;
    public float lengthVariance;
    public float muzzleFlashDuration;
    public float elapsedTime;
    public float startScale;
    public float endScale;
    public float startLength;
    public float endLength;
    public bool isInitialized;
    public float sfxPitch;
    public float sfxVolume;
}


