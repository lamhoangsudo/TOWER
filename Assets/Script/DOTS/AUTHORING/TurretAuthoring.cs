using Unity.Entities;
using UnityEngine;

public class TurretAuthoring : MonoBehaviour
{
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

    public GameObject headingPivot;
    public GameObject elevationPivot;

    public float currentHeading;
    public float currentElevation;
    public float currentHeadingSpeed;
    public float currentElevationSpeed;

    public GameObject target;
    public bool useTargetPrediction;
    public bool autoFire;
    public float targetAquiredAngle;
    public bool resetOrientation;
    public class TurretAuthoringBaker : Baker<TurretAuthoring>
    {
        public override void Bake(TurretAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Turret
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
                currentHeading = authoring.currentHeading,
                currentElevation = authoring.currentElevation,
                currentHeadingSpeed = authoring.currentHeadingSpeed,
                currentElevationSpeed = authoring.currentElevationSpeed,
                target = GetEntity(authoring.target, TransformUsageFlags.Dynamic),
                useTargetPrediction = authoring.useTargetPrediction,
                autoFire = authoring.autoFire,
                targetAquiredAngle = authoring.targetAquiredAngle,
                resetOrientation = authoring.resetOrientation,
            });
        }
    }
}
public struct Turret : IComponentData
{
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

    public Entity headingPivot;
    public Entity elevationPivot;

    public float currentHeading;
    public float currentElevation;
    public float currentHeadingSpeed;
    public float currentElevationSpeed;

    public Entity target;
    public bool useTargetPrediction;
    public bool autoFire;
    public float targetAquiredAngle; // 0.5
    public bool resetOrientation;

    public float headingSpeedFactor;
    public float elevationSpeedFactor;

    public bool IsHeadingRotationSFX;
    public bool IsElevationRotationSFX;
}


