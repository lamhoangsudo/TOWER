using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public Enum.ProjectileType projectileType;
    public float homingSpeed;
    public class ProjectileAuthoringBaker : Baker<ProjectileAuthoring>
    {
        
        public override void Bake(ProjectileAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Projectile
            {
                projectileType = authoring.projectileType,
                homingSpeed = authoring.homingSpeed,
            });
        }
    }
}
public struct Projectile : IComponentData
{
    public float projectileCurrentSpeed;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public float projectileLifetimeMax;
    public float projectileCurrentLifetime;
    public float3 direction;
    public Entity homingTarget;
    public float homingSpeed;
    public float targetDistance;
    public bool usePrediction;
    public int impactLayer;
    public Entity projectileExplosion;
    public Enum.ProjectileType projectileType;
    public float timeDelayRayMax;
    public float timeDelayRay;

    public float dot;
    public float angle;
    public float timeRotation;

    /// <summary>
    /// Vị trí frame trước — dùng cho anti-tunneling raycast.
    /// </summary>
    public float3 previousPosition;
}
