using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Entities;
using UnityEngine;
public class SFX_HeadingAuthoring : MonoBehaviour
{
    public TurretAuthoring turretAuthoring;
    public EventReference soundEventReference;
    public Entity SFX_HeadingEntity { get; private set; }
    public class SFX_HeadingAuthoringBaker : Baker<SFX_HeadingAuthoring>
    {
        public override void Bake(SFX_HeadingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SFX_Heading
            {
                turretEntity = GetEntity(authoring.turretAuthoring.gameObject, TransformUsageFlags.Dynamic),
                soundEventReferenceGUID = authoring.soundEventReference.Guid,
            });
            authoring.SFX_HeadingEntity = entity;
        }
    }
}
public struct SFX_Heading : IComponentData
{
    public Entity turretEntity;
    public float headingSpeedFactor;
    public bool isPlaying;
    public GUID soundEventReferenceGUID;
}


