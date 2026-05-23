using Unity.Entities;

/// <summary>
/// Barrel VFX data: muzzle flash + point shot references.
/// Gắn trên weapon entity. BarrelAnimationSystem spawn flash khi bắn.
/// </summary>
public struct BarrelVFX : IComponentData
{
    public Entity muzzleFlashEntity;
    public bool flashSpawned;

    // Blob chứa point shot entity references (vị trí spawn projectile + flash)
    public BlobAssetReference<PointShotEntityBlobDatabase> pointShotBlob;
}
