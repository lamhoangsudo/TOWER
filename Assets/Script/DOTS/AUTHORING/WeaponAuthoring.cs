using Unity.Entities;
using UnityEngine;

public class WeaponAuthoring : MonoBehaviour
{
    public Enum.WeaponFiringPattern firingPattern;
    public float spreadAngle;

    public float gatlingRotationSpeed;

    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float projectileLifetime;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public bool projectileUsePrediction;
    public int impactLayer;
    public class WeaponAuthoringBaker : Baker<WeaponAuthoring>
    {

        public override void Bake(WeaponAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Weapon
            {
                firingPattern = authoring.firingPattern,
                spreadAngle = authoring.spreadAngle,
                gatlingRotationSpeed = authoring.gatlingRotationSpeed,
                projectilePrefab = GetEntity(authoring.projectilePrefab, TransformUsageFlags.Dynamic),
                projectileSpeed = authoring.projectileSpeed,
                projectileLifetime = authoring.projectileLifetime,
                projectileAcceleration = authoring.projectileAcceleration,
                projectileMaxSpeed = authoring.projectileMaxSpeed,
                projectileUsePrediction = authoring.projectileUsePrediction,
                impactLayer = authoring.impactLayer,
                startFire = false,
            });
        }
    }
}
public struct Weapon : IComponentData
{
    public Enum.WeaponFiringPattern firingPattern;
    public float spreadAngle;
    public float gatlingRotationSpeed;
    public Entity projectilePrefab;
    public float projectileSpeed;
    public float projectileLifetime;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public bool projectileUsePrediction;
    public int impactLayer;
    public bool startFire;
    public bool startGatling;
}


