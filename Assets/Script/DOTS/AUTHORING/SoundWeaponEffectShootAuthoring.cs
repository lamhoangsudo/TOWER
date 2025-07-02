using Unity.Entities;
using UnityEngine;

public class SoundWeaponEffectShootAuthoring : MonoBehaviour
{
    public class SoundWeaponEffectShootAuthoringBaker : Baker<SoundWeaponEffectShootAuthoring>
    {
        public override void Bake(SoundWeaponEffectShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoundWeaponEffectShoot
            {
                isPlayOneShot = false,
                volume = 1.0f,
                pitch = 1.0f,
            });
        }
    }
}
public struct SoundWeaponEffectShoot : IComponentData
{
    public bool isPlayOneShot;
    public float volume;
    public float pitch;
}


