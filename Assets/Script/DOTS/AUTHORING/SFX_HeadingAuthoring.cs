using Unity.Entities;
using UnityEngine;

public class SFX_HeadingAuthoring : MonoBehaviour
{
    public TurretAuthoring turretAuthoring;
    public Entity SFX_HeadingEntity { get; private set; }
    public class SFX_HeadingAuthoringBaker : Baker<SFX_HeadingAuthoring>
    {
        public override void Bake(SFX_HeadingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AudioSource audioSource = authoring.GetComponent<AudioSource>();
            AddComponent(entity, new SFX_Heading
            {
                headingRotationSFXInitialPitch = audioSource.pitch,
                headingRotationSFXInitialVolume = audioSource.volume,
                turretEntity = GetEntity(authoring.turretAuthoring.gameObject, TransformUsageFlags.Dynamic),
            });
            authoring.SFX_HeadingEntity = entity;
        }
    }
}
public struct SFX_Heading : IComponentData
{
    public Entity turretEntity;
    public float headingRotationSFXInitialPitch;
    public float headingRotationSFXInitialVolume;
    public bool isPlaying;
}


