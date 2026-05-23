using Unity.Entities;

/// <summary>
/// Component data cho turret firing decisions.
/// TurretFireSystem đọc targeting.isHeadingRotationTarget + isElevationRotationTarget
/// để quyết định có bắn hay không.
/// </summary>
public struct TurretFiring : IComponentData
{
    public bool autoFire;

    // Random cho sound pitch variation (giữ từ struct Turret cũ)
    public Unity.Mathematics.Random random;
}
