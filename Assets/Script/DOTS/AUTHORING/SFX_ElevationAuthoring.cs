using Unity.Entities;
using UnityEngine;

public class SFX_ElevationAuthoring : MonoBehaviour
{
    public Entity SFX_HeadingEntity { get; private set; }
    public class SFX_ElevationAuthoringBaker : Baker<SFX_ElevationAuthoring>
    {
        public override void Bake(SFX_ElevationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SFX_Elevation
            {
                random = new Unity.Mathematics.Random((uint)entity.Index),
            });
            authoring.SFX_HeadingEntity = entity;
        }
    }
}
public struct SFX_Elevation : IComponentData
{
    public float ElevationRotationSFXInitialPitch;
    public float ElevationRotationSFXInitialVolume;
    public bool isPlaying;
    public Unity.Mathematics.Random random;
}


