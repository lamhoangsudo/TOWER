using Unity.Entities;

/// <summary>
/// Component data cho turret targeting.
/// TurretTargetingSystem tính headingAim/elevationAim từ target position.
/// Rotation systems đọc aim angles để xoay.
/// </summary>
public struct TurretTargeting : IComponentData
{
    public Entity target;
    public bool useTargetPrediction;
    public bool resetOrientation;

    // Aim angles (tính bởi TurretTargetingSystem)
    public float headingAim;
    public float elevationAim;

    // Được set bởi Heading/Elevation systems khi deltaAngle <= targetAcquiredAngle
    public bool isHeadingRotationTarget;
    public bool isElevationRotationTarget;

    // Ngưỡng góc để coi là "đã aim đúng"
    public float targetAcquiredAngle;
}
