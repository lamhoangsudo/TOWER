using Unity.Entities;

/// <summary>
/// Core barrel animation data: recoil slide + tip rotation.
/// Gắn trên weapon entity. BarrelAnimationSystem đọc component này.
/// </summary>
public struct BarrelAnimation : IComponentData
{
    public Entity barrelBaseEntity;

    // Animation settings
    public float animationDuration;
    public float baseSlideDistance;
    public float tipSlideAmountDistance;
    public float tipRotateDegrees;

    // Animation state
    public float lastFireTime;
    public bool animationPlaying;
    public float tipRotationAtFire;

    // Baked curve data (sampled from AnimationCurve)
    public BlobAssetReference<BarrelAnimatorCurveBlobDatabase> curveBlob;
}
