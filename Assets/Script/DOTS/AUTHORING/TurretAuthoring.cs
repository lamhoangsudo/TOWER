using NUnit.Framework;
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
    public Entity turretEntity;

    public GameObject SFX_Heading;
    public GameObject SFX_Elevation;
    public GameObject[] weapons;
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
                IsElevationRotationSFX = false,
                IsHeadingRotationSFX = false,
                random = new Unity.Mathematics.Random((uint)entity.Index),
                SFX_HeadingEntity = GetEntity(authoring.SFX_Heading, TransformUsageFlags.None),
                SFX_ElevationEntity = GetEntity(authoring.SFX_Elevation, TransformUsageFlags.None),
                isElevationRotationTarget = false,
                isHeadingRotationTarget = false,
            });
            foreach (GameObject weapon in authoring.weapons)
            {
                Entity weaponEntity = GetEntity(weapon, TransformUsageFlags.Dynamic);
                AddBuffer<WeaponItemBuffer>(entity).Add(new WeaponItemBuffer { weaponEntity = weaponEntity });
            }
            authoring.turretEntity = entity;
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
    public bool isHeadingRotationTarget;
    public bool isElevationRotationTarget;

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
    public Unity.Mathematics.Random random; // Used for sound pitch variation

    public Entity SFX_HeadingEntity;
    public Entity SFX_ElevationEntity;
}
[InternalBufferCapacity(8)]
public struct WeaponItemBuffer : IBufferElementData
{
    public Entity weaponEntity;
}


