using Unity.Entities;
using UnityEngine;

public class WeaponAuthoring : MonoBehaviour
{
    public Enum.WeaponFiringPattern firingPattern;
    public float gatlingRotationSpeed;
    [Header("Projec Tile Set Up")]
    public GameObject projectilePrefab;
    public float projectileMaxLifetime;
    public float projectileMaxSpeed;
    public int impactLayer;
    [Header("Projec Tile Missile Set Up")]
    public float projectileStartSpeed;
    public float projectileAcceleration;
    public bool projectileUsePrediction;
    [Header("Projec Tile Gatling Set Up")]
    public float spreadAngle;
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
                projectileStartSpeed = authoring.projectileStartSpeed,
                projectileMaxLifetime = authoring.projectileMaxLifetime,
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
    public float projectileStartSpeed;
    public float projectileMaxLifetime;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public bool projectileUsePrediction;
    public int impactLayer;
    public bool startFire;
    public bool startGatling;
}


