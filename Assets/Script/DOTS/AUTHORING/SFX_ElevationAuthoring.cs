using Unity.Entities;
using UnityEditor.PackageManager;
using UnityEngine;

public class SFX_ElevationAuthoring : MonoBehaviour
{
    public TurretAuthoring turretAuthoring;
    public Entity SFX_HeadingEntity { get; private set; }
    public class SFX_ElevationAuthoringBaker : Baker<SFX_ElevationAuthoring>
    {
        public override void Bake(SFX_ElevationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AudioSource audioSource = authoring.GetComponent<AudioSource>();
            AddComponent(entity, new SFX_Elevation
            {
                elevationRotationSFXInitialPitch = audioSource.pitch,
                elevationRotationSFXInitialVolume = audioSource.volume,
                turretEntity = GetEntity(authoring.turretAuthoring.gameObject, TransformUsageFlags.Dynamic),
            });
            authoring.SFX_HeadingEntity = entity;
        }
    }
}
public struct SFX_Elevation : IComponentData
{
    public Entity turretEntity;
    public float elevationRotationSFXInitialPitch;
    public float elevationRotationSFXInitialVolume;
    public bool isPlaying;
}


