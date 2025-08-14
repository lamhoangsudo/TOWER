using UnityEngine;

public static class Enum
{
    public enum TurretFiringPattern
    {
        Individual,
        Simultaneous,
        Gatling,
    }
    public enum WeaponFiringPattern
    {
        Individual,
        Simultaneous,
        Gatling,
        MissileLauncher
    }
    public enum ProjectTileType
    {
        Bullet,
        Missile,
    }
    public enum PlatformSizeType
    {
        Small,
        Medium,
        Large,
    }
    public enum BuildingID
    {
        None,
        Platform_L,
        Platform_M,
        Platform_S,
    }
    public enum SnapPointType
    {
        None,
        PlatformSnap,
        TurretSnap,
    }
    public enum Direction
    {
        none,
        up,
        down,
        left,
        right,
        forward,
        backward,
    }
}
