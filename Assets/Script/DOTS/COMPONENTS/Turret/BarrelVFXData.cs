using Unity.Entities;

/// <summary>
/// Barrel VFX data: muzzle flash reference + flash state.
/// Gắn trên weapon entity. BarrelFireEffectSystem spawn flash khi bắn.
/// Point shot entities giờ nằm trong DynamicBuffer PointShotEntityBuffer.
/// </summary>
public struct BarrelVFX : IComponentData
{
    public Entity muzzleFlashEntity;
    public bool flashSpawned;
}

/// <summary>
/// Buffer chứa point shot entity references (vị trí spawn projectile + flash).
/// Dùng DynamicBuffer thay vì BlobAsset để entity references được remap đúng.
/// </summary>
[InternalBufferCapacity(4)]
public struct PointShotEntityBuffer : IBufferElementData
{
    public Entity pointShoot;
}
