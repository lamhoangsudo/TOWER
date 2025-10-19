
public static class Enum
{
    public enum BuildType
    {
        None,
        Platform,
        Turret,
    }
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
    public enum PointBuidStatus
    {
        none = 0,
        validPointBuid = 1,
        unvalidPointBuid = 2,
    }
    public enum BuidingState
    {
        none = 0,
        freestyle = 1,
        gridstyle = 2,
    }
}
