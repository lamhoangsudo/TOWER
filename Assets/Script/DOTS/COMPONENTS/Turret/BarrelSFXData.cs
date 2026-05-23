using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Barrel SFX data: pitch, volume, random state cho sound variation.
/// Gắn trên weapon entity. BarrelAnimationSystem dùng khi trigger fire sound.
/// </summary>
public struct BarrelSFX : IComponentData
{
    public float sfxPitch;
    public float sfxVolume;
    public Random random;
}
