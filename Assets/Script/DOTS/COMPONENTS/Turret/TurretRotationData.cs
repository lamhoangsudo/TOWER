using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Component data cho turret rotation.
/// Lưu angle dạng float (tránh quaternion drift).
/// Heading/Elevation pivot là entities riêng.
/// </summary>
public struct TurretRotation : IComponentData
{
    // Pivot entity references
    public Entity headingPivot;
    public Entity elevationPivot;

    // Current state
    public float currentHeading;
    public float currentElevation;
    public float currentHeadingSpeed;
    public float currentElevationSpeed;

    // Settings
    public float headingRotationSpeed;
    public float headingRotationAcceleration;
    public float elevationRotationSpeed;
    public float elevationRotationAcceleration;

    // Constraints
    public float minHeadingLimit;
    public float maxHeadingLimit;
    public bool headingLimited;
    public float minElevationLimit;
    public float maxElevationLimit;
    public bool elevationLimited;

    // SFX factors (computed by rotation systems, read by sound system)
    public float headingSpeedFactor;
    public float elevationSpeedFactor;
    public bool IsHeadingRotationSFX;
    public bool IsElevationRotationSFX;
}
