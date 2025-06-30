using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public float speed;
    public float acceleration;
    public float maxSpeed;
    public float lifetime;
    public float3 direction;

    public GameObject homingTarget;
    public float homingStrength;

    public bool usePrediction;

    public int impactLayer;

    public float timeAlive;

    public GameObject projectileGO;
    public class ProjectileAuthoringBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Projectile
            {
                speed = authoring.speed,
                acceleration = authoring.acceleration,
                maxSpeed = authoring.maxSpeed,
                lifetime = authoring.lifetime,
                direction = authoring.direction,
                homingTarget = GetEntity(authoring.homingTarget, TransformUsageFlags.Dynamic),
                homingStrength = authoring.homingStrength,
                usePrediction = authoring.usePrediction,
                impactLayer = authoring.impactLayer,
                timeAlive = authoring.timeAlive,
                projectileGO = GetEntity(authoring.projectileGO, TransformUsageFlags.Dynamic)
            });
        }
    }
}
public struct Projectile : IComponentData
{
    public float speed;
    public float acceleration;
    public float maxSpeed;
    public float lifetime;
    public float3 direction;

    public Entity homingTarget;
    public float homingStrength;

    public bool usePrediction;

    public int impactLayer;

    public float timeAlive;

    public Entity projectileGO;
}


