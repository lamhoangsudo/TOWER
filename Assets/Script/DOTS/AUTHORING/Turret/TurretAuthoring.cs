using Unity.Entities;
using UnityEngine;

public class TurretAuthoring : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float headingRotationSpeed;
    public float headingRotationAcceleration;
    public float minHeadingLimit;
    public float maxHeadingLimit;
    public bool headingLimited;
    public float elevationRotationSpeed;
    public float elevationRotationAcceleration;
    public float minElevationLimit;
    public float maxElevationLimit;
    public bool elevationLimited;

    [Header("Pivot References")]
    public GameObject headingPivot;
    public GameObject elevationPivot;

    [Header("Targeting")]
    public bool useTargetPrediction;
    public float targetAquiredAngle;
    public bool resetOrientation;

    [Header("Firing")]
    public bool autoFire;

    public Entity turretEntity { get; private set; }

    [Header("Debug")]
    public GameObject Target;

    public class TurretAuthoringBaker : Baker<TurretAuthoring>
    {
        public override void Bake(TurretAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Component 1: Rotation
            AddComponent(entity, new TurretRotation
            {
                headingRotationSpeed = authoring.headingRotationSpeed,
                headingRotationAcceleration = authoring.headingRotationAcceleration,
                minHeadingLimit = authoring.minHeadingLimit,
                maxHeadingLimit = authoring.maxHeadingLimit,
                headingLimited = authoring.headingLimited,
                elevationRotationSpeed = authoring.elevationRotationSpeed,
                elevationRotationAcceleration = authoring.elevationRotationAcceleration,
                minElevationLimit = authoring.minElevationLimit,
                maxElevationLimit = authoring.maxElevationLimit,
                elevationLimited = authoring.elevationLimited,
                headingPivot = GetEntity(authoring.headingPivot, TransformUsageFlags.Dynamic),
                elevationPivot = GetEntity(authoring.elevationPivot, TransformUsageFlags.Dynamic),
                IsHeadingRotationSFX = false,
                IsElevationRotationSFX = false,
            });

            // Component 2: Targeting
            AddComponent(entity, new TurretTargeting
            {
                target = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
                useTargetPrediction = authoring.useTargetPrediction,
                resetOrientation = authoring.resetOrientation,
                targetAcquiredAngle = authoring.targetAquiredAngle,
                isHeadingRotationTarget = false,
                isElevationRotationTarget = false,
            });

            // Component 3: Firing
            AddComponent(entity, new TurretFiring
            {
                autoFire = authoring.autoFire,
                random = new Unity.Mathematics.Random((uint)entity.Index),
            });

            authoring.turretEntity = entity;
        }
    }
}


