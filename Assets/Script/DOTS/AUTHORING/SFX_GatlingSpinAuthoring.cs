using Unity.Entities;
using UnityEngine;

public class SFX_GatlingSpinAuthoring : MonoBehaviour
{
    public AudioSource audioSource;
    public class SFX_GatlingSpinAuthoringBaker : Baker<SFX_GatlingSpinAuthoring>
    {
        public override void Bake(SFX_GatlingSpinAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SFX_GatlingSpin
            {
                isPlaying = false,
                gatlingSpinAudioPitch = authoring.audioSource.pitch,
                gatlingSpinAudioVolume = authoring.audioSource.volume,
            });
        }
    }
}
public struct SFX_GatlingSpin : IComponentData 
{
    public bool isPlaying;
    public float gatlingSpinAudioPitch;
    public float gatlingSpinAudioVolume;
    public float gatlingRotationFactor;
    public float curentGatlingRotation;
}


