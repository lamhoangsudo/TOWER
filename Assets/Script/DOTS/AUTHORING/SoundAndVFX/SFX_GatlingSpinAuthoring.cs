using FMOD;
using FMODUnity;
using Unity.Entities;
using UnityEngine;

public class SFX_GatlingSpinAuthoring : MonoBehaviour
{
    public EventReference soundEventReferenceGatlingSpin;
    public class SFX_GatlingSpinAuthoringBaker : Baker<SFX_GatlingSpinAuthoring>
    {
        public override void Bake(SFX_GatlingSpinAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SFX_GatlingSpin
            {
                isPlaying = false,
                soundEventReferenceGatlingSpinGUID = authoring.soundEventReferenceGatlingSpin.Guid,
            });
        }
    }
}
public struct SFX_GatlingSpin : IComponentData 
{
    public bool isPlaying;
    public float gatlingRotationFactor;
    public GUID soundEventReferenceGatlingSpinGUID;
}


