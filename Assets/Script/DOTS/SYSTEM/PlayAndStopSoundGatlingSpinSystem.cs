using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

partial struct PlayAndStopSoundGatlingSpinSystem : ISystem
{
    private float GatlingSpinSFXInitialPitch;
    private float GatlingSpinSFXInitialVolume;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<SFX_GatlingSpin> sfx_GatlingSpin, Entity entity) in SystemAPI.Query<RefRO<SFX_GatlingSpin>>().WithEntityAccess())
        {
            GatlingSpinSFXInitialPitch = 2f;
            GatlingSpinSFXInitialVolume = sfx_GatlingSpin.ValueRO.gatlingSpinAudioVolume;
            AudioSource audioSource = state.World.EntityManager.GetComponentObject<AudioSource>(entity);
            if (sfx_GatlingSpin.ValueRO.isPlaying)
            {
                if (!audioSource.isPlaying) audioSource.Play();
                audioSource.pitch = sfx_GatlingSpin.ValueRO.gatlingSpinAudioPitch * sfx_GatlingSpin.ValueRO.gatlingRotationFactor;
                audioSource.volume = math.lerp(0f, 1f, sfx_GatlingSpin.ValueRO.gatlingRotationFactor);
            }
            else
            {
                if (audioSource.isPlaying) audioSource.Stop();
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
