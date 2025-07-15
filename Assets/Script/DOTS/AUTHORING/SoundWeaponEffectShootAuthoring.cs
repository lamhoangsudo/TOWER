using FMOD;
using FMODUnity;
using Unity.Entities;
using UnityEngine;

public class SoundWeaponEffectShootAuthoring : MonoBehaviour
{
    public EventReference soundEventReferenceSoundWeaponEffectShoot;
    public class SoundWeaponEffectShootAuthoringBaker : Baker<SoundWeaponEffectShootAuthoring>
    {
        public override void Bake(SoundWeaponEffectShootAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoundWeaponEffectShoot
            {
                isPlayOneShot = false,
                soundEventReferenceSoundWeaponEffectShootGUID = authoring.soundEventReferenceSoundWeaponEffectShoot.Guid,
            });
        }
    }
}
public struct SoundWeaponEffectShoot : IComponentData
{
    public bool isPlayOneShot;
    public float volume;
    public float pitch;
    public GUID soundEventReferenceSoundWeaponEffectShootGUID;
}


