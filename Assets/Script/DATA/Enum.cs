
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
    public enum ProjectileType
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
        TestBuilding,
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
    public enum PlacementMode
    {
        none = 0,
        freestyle = 1,
        gridstyle = 2,
    }
    public enum BuildingMode
    {
        none = 0,
        single_grid = 1,
        area_grid = 2,
        line = 3,
        single_free = 4,
        area_free = 5,
    }
    public enum BuildRotationDirection
    {
        up = 0,
        right = 90,
        down = -180,
        left = -90,
    }
}
