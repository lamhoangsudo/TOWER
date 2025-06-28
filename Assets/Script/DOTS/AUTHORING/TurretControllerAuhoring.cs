using Unity.Entities;
using UnityEngine;
/// <summary>
/// ECS data for turret aiming and tracking.
/// </summary>
public class TurretControllerAuhoring : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 90f;

    [Tooltip("GameObject that rotates left-right (yaw)")]
    public GameObject turretHeadingParent;

    [Tooltip("GameObject that rotates up-down (pitch)")]
    public GameObject turretElevationParent;
    public class TurretControllerAuhoringBaker : Baker<TurretControllerAuhoring>
    {
        public override void Bake(TurretControllerAuhoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TurretController
            {
                rotationSpeed = authoring.rotationSpeed,
                targetEntity = Entity.Null,
                turretHeadingEntity = GetEntity(authoring.turretHeadingParent, TransformUsageFlags.Dynamic),
                turretElevationEntity = GetEntity(authoring.turretElevationParent, TransformUsageFlags.Dynamic)
            });
        }
    }
}
public struct TurretController : IComponentData 
{
    public float rotationSpeed;             // degrees per second
    public Entity targetEntity;             // entity to track
    public Entity turretHeadingEntity;      // entity rotating yaw (left-right)
    public Entity turretElevationEntity;    // entity rotating pitch (up-down)
}



