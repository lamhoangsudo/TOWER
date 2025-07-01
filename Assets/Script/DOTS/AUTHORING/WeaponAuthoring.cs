using Unity.Entities;
using UnityEngine;

public class WeaponAuthoring : MonoBehaviour
{
    public WeaponFiringPattern firingPattern;
    public int burstShots;
    public float burstDelay;
    public float cooldown;
    public float spreadAngle;

    public float gatlingRotationSpeed;

    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float projectileLifetime;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public bool projectileUsePrediction;
    public int impactLayer;

    public float sfxPitch;
    public float sfxVolume;

    public bool isFiring;
    public float currentCooldown;
    public int burstCounter;
    public GameObject[] BarrelAnimator;
    public class WeaponAuthoringBaker : Baker<WeaponAuthoring>
    {

        public override void Bake(WeaponAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Weapon
            {
                firingPattern = authoring.firingPattern,
                burstShots = authoring.burstShots,
                burstDelay = authoring.burstDelay,
                cooldown = authoring.cooldown,
                spreadAngle = authoring.spreadAngle,
                gatlingRotationSpeed = authoring.gatlingRotationSpeed,
                projectilePrefab = GetEntity(authoring.projectilePrefab, TransformUsageFlags.Dynamic),
                projectileSpeed = authoring.projectileSpeed,
                projectileLifetime = authoring.projectileLifetime,
                projectileAcceleration = authoring.projectileAcceleration,
                projectileMaxSpeed = authoring.projectileMaxSpeed,
                projectileUsePrediction = authoring.projectileUsePrediction,
                impactLayer = authoring.impactLayer,
                sfxPitch = authoring.sfxPitch,
                sfxVolume = authoring.sfxVolume,
                isFiring = authoring.isFiring,
                currentCooldown = authoring.currentCooldown,
                burstCounter = authoring.burstCounter,
                random = new Unity.Mathematics.Random((uint)entity.Index)
            });
            foreach(GameObject gameObject in authoring.BarrelAnimator)
            {
                Entity barrelAnimatorEntity = GetEntity(gameObject, TransformUsageFlags.Dynamic);
                AddBuffer<BarrelAnimatorBuffer>(entity).Add(new BarrelAnimatorBuffer { barrelAnimatorBuffer = barrelAnimatorEntity });
            }
        }
    }
}
public enum WeaponFiringPattern
{
    Individual,
    Simultaneous,
    Gatling,
    MissileLauncher
}
public struct Weapon : IComponentData
{
    public WeaponFiringPattern firingPattern;
    public int burstShots;
    public float burstDelay;
    public float cooldown;
    public float spreadAngle;

    public float gatlingRotationSpeed;

    public Entity projectilePrefab;
    public float projectileSpeed;
    public float projectileLifetime;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public bool projectileUsePrediction;
    public int impactLayer;

    public float sfxPitch;
    public float sfxVolume;

    public bool isFiring;
    public float currentCooldown;
    public int burstCounter;
    public Unity.Mathematics.Random random;
}
[InternalBufferCapacity(6)]
public struct BarrelAnimatorBuffer : IBufferElementData
{
    public Entity barrelAnimatorBuffer;
}


