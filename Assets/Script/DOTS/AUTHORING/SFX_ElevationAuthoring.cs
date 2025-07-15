using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Entities;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class SFX_ElevationAuthoring : MonoBehaviour
{
    public TurretAuthoring turretAuthoring;
    public EventReference soundEventReference;
    public Entity SFX_HeadingEntity { get; private set; }
    public class SFX_ElevationAuthoringBaker : Baker<SFX_ElevationAuthoring>
    {
        public override void Bake(SFX_ElevationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SFX_Elevation
            {
                turretEntity = GetEntity(authoring.turretAuthoring.gameObject, TransformUsageFlags.Dynamic),
                soundEventReferenceGUID = authoring.soundEventReference.Guid,
            });
            authoring.SFX_HeadingEntity = entity;
        }
    }
}
public struct SFX_Elevation : IComponentData
{
    public Entity turretEntity;
    public float elevationSpeedFactor;
    public bool isPlaying;
    public GUID soundEventReferenceGUID;
}


