using FMOD.Studio;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using System.Collections.Generic;
[UpdateAfter(typeof(BarrelAnimatorSystem))]
public partial class PlayAndStopSoundGatlingSpinSystem : SystemBase
{
    private Dictionary<Entity, EventInstance> _SoundGatlingSpinEventInstanceDictionary = new();
    [BurstCompile]
    protected override void OnCreate()
    {

    }
    protected override void OnUpdate()
    {
        foreach ((RefRO<SFX_GatlingSpin> sfx_GatlingSpin, RefRO<LocalToWorld> localToWorld, Entity entity) in SystemAPI.Query<RefRO<SFX_GatlingSpin>, RefRO<LocalToWorld>>().WithEntityAccess())
        {
            if (!_SoundGatlingSpinEventInstanceDictionary.ContainsKey(entity))
            {
                EventInstance eventInstance = FmodSoundManager.GetEventInstance(sfx_GatlingSpin.ValueRO.soundEventReferenceGatlingSpinGUID);
                FmodSoundManager.SetPositionEventInstance(eventInstance, localToWorld.ValueRO.Position);
                _SoundGatlingSpinEventInstanceDictionary.Add(entity, eventInstance);
            }
            else
            {
                EventInstance eventInstance = _SoundGatlingSpinEventInstanceDictionary[entity];
                if (sfx_GatlingSpin.ValueRO.isPlaying)
                {
                    FmodSoundManager.SetParameterSoundEffectLoop(eventInstance, "SpeedGatlingSpinSpeedFactor", sfx_GatlingSpin.ValueRO.gatlingRotationFactor);
                    FmodSoundManager.PlaySoundEffectLoop(eventInstance);
                }
                else
                {
                    FmodSoundManager.StopSoundEffectLoop(eventInstance, STOP_MODE.ALLOWFADEOUT);
                }
            }
        }
    }

    [BurstCompile]
    protected override void OnDestroy()
    {

    }
}
