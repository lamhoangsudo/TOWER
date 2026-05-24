using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileSpawnShootAuthoring : MonoBehaviour
{
    public class ProjectileSpawnShootAuthoringBaker : Baker<ProjectileSpawnShootAuthoring>
    {
        public override void Bake(ProjectileSpawnShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ProjectileSpawnShoot
            {
            });
        }
    }
}
public struct ProjectileSpawnShoot : IComponentData
{
    public Enum.WeaponFiringPattern firingPattern;
    public bool isSpawner;
    public Entity entityProjectilePrefab;
    public Entity entityProjectileExplosion;
    public float projectileStartSpeed;
    public float projectileLifetimeMax;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public float3 targetPosition;
    public Entity homingTarget;
}
