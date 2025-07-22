using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectTileAuthoring : MonoBehaviour
{
    public Enum.ProjectTileType projectTileType;
    public float homingSpeed;
    public class ProjectTileAuthoringBaker : Baker<ProjectTileAuthoring>
    {
        
        public override void Bake(ProjectTileAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ProjecTile
            {
                projectTileType = authoring.projectTileType,
                homingSpeed = authoring.homingSpeed,
            });
        }
    }
}
public struct ProjecTile : IComponentData
{
    public float projecTileCurrentSpeed;
    public float projecTileAcceleration;
    public float projecTileMaxSpeed;
    public float projecTileLifetimeMax;
    public float projecTileCurrentLifetime;
    public float3 direction;
    public Entity homingTarget;
    public float homingSpeed;
    public float targetDistance;
    public bool usePrediction;
    public int impactLayer;
    public Entity projectileGO;
    public Enum.ProjectTileType projectTileType;
    public float timeDelayRayMax;
    public float timeDelayRay;

    public float dot;
    public float angle;
    public float timeRotation;
}


