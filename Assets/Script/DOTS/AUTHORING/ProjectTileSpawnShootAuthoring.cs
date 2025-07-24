using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectTileSpawnShootAuthoring : MonoBehaviour
{
    public class ProjectTileSpawnShootAuthoringBaker : Baker<ProjectTileSpawnShootAuthoring>
    {
        public override void Bake(ProjectTileSpawnShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ProjectTileSpawnShoot
            {
            });
        }
    }
}
public struct ProjectTileSpawnShoot : IComponentData
{
    public Enum.WeaponFiringPattern firingPattern;
    public bool isSpawner;
    public Entity entityProjectTilePrefab;
    public Entity entityProjectTileExplosion;
    public float projectileStartSpeed;
    public float projectileLifetimeMax;
    public float projectileAcceleration;
    public float projectileMaxSpeed;
    public float3 targetPosition;
    public Entity homingTarget;
}


