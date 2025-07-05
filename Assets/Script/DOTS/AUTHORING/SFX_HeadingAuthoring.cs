using Unity.Entities;
using UnityEngine;

public class SFX_HeadingAuthoring : MonoBehaviour
{
    public Entity SFX_HeadingEntity { get; private set; }
    public class SFX_HeadingAuthoringBaker : Baker<SFX_HeadingAuthoring>
    {
        public override void Bake(SFX_HeadingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SFX_Heading
            {
                random = new Unity.Mathematics.Random((uint)entity.Index),
            });
            authoring.SFX_HeadingEntity = entity;
        }
    }
}
public struct SFX_Heading : IComponentData
{
    public float HeadingRotationSFXInitialPitch;
    public float HeadingRotationSFXInitialVolume;
    public bool isPlaying;
    public Unity.Mathematics.Random random;
}


