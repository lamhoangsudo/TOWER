using FMOD.Studio;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
public partial class PlayAndStopSoundHeadingSystem : SystemBase
{
    private Dictionary<Entity, EventInstance> _SoundHeadingEventInstanceDictionary = new();
    private List<Entity> _entitiesToRemove = new();

    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        // Cleanup: release instances cho entities đã bị destroy
        if (_SoundHeadingEventInstanceDictionary.Count > 0)
        {
            _entitiesToRemove.Clear();
            foreach (var kvp in _SoundHeadingEventInstanceDictionary)
            {
                if (!EntityManager.Exists(kvp.Key))
                {
                    FmodSoundManager.ReleaseEventInstance(kvp.Value);
                    _entitiesToRemove.Add(kvp.Key);
                }
            }
            foreach (var entity in _entitiesToRemove)
            {
                _SoundHeadingEventInstanceDictionary.Remove(entity);
            }
        }

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

                // Update 3D position mỗi frame (turret có thể di chuyển)
                FmodSoundManager.SetPositionEventInstance(eventInstance, localToWorld.ValueRO.Position);

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
        // Release tất cả FMOD instances khi system bị destroy
        foreach (var kvp in _SoundHeadingEventInstanceDictionary)
        {
            FmodSoundManager.ReleaseEventInstance(kvp.Value);
        }
        _SoundHeadingEventInstanceDictionary.Clear();
    }
}
