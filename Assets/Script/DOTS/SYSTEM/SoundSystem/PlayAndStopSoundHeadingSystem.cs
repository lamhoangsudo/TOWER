using FMOD.Studio;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using System.Collections.Generic;
[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
public partial class PlayAndStopSoundHeadingSystem : SystemBase
{
    private Dictionary<Entity, EventInstance> _SoundHeadingEventInstanceDictionary = new();
    protected override void OnCreate()
    {

    }
    protected override void OnUpdate()
    {
        foreach ((RefRW<SFX_Heading> sfx_Heading, RefRO<LocalToWorld> localToWorld, Entity entity) in SystemAPI.Query<RefRW<SFX_Heading>, RefRO<LocalToWorld>>().WithEntityAccess())
        {
            if (!_SoundHeadingEventInstanceDictionary.ContainsKey(entity))
            {
                EventInstance eventInstance = FmodSoundManager.GetEventInstance(sfx_Heading.ValueRO.soundEventReferenceGUID);
                FmodSoundManager.SetPositionEventInstance(eventInstance, localToWorld.ValueRO.Position);
                _SoundHeadingEventInstanceDictionary.Add(entity, eventInstance);
            }
            else
            {
                EventInstance eventInstance = _SoundHeadingEventInstanceDictionary[entity];
                if (sfx_Heading.ValueRO.isPlaying)
                {
                    FmodSoundManager.SetParameterSoundEffectLoop(eventInstance, "SpeedHeadingFactor", sfx_Heading.ValueRO.headingSpeedFactor);
                    FmodSoundManager.PlaySoundEffectLoop(eventInstance);
                }
                else
                {
                    FmodSoundManager.StopSoundEffectLoop(eventInstance, STOP_MODE.ALLOWFADEOUT);
                }
            }
        }
    }
    protected override void OnDestroy()
    {

    }
}
