using FMOD;
using FMODUnity;
using Unity.Entities;
using UnityEngine;

public class ExplosionSFXAuthoring : MonoBehaviour
{
    public EventReference soundEventReferenceExplosionSFX;
    public float volume;
    public float pitch;
    public class ExplosionSFXAuthoringBaker : Baker<ExplosionSFXAuthoring>
    {
        public override void Bake(ExplosionSFXAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExplosionSFX
            {
                isPlayShoot = true,
                pitch = authoring.pitch,
                volume = authoring.volume,
                soundEventReferenceExplosionSFXGUID = authoring.soundEventReferenceExplosionSFX.Guid,
            });
        }
    }
}
public struct ExplosionSFX : IComponentData
{
    public bool isPlayShoot;
    public float volume;
    public float pitch;
    public GUID soundEventReferenceExplosionSFXGUID;
}


