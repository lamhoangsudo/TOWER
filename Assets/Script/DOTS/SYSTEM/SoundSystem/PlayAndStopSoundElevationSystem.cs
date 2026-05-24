using FMOD.Studio;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
public partial class PlayAndStopSoundElevationSystem : SystemBase
{
    private Dictionary<Entity, EventInstance> _SoundElevationEventInstanceDictionary = new();
    private List<Entity> _entitiesToRemove = new();

    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        // Cleanup: release instances cho entities đã bị destroy
        if (_SoundElevationEventInstanceDictionary.Count > 0)
        {
            _entitiesToRemove.Clear();
            foreach (var kvp in _SoundElevationEventInstanceDictionary)
            {
                if (!EntityManager.Exists(kvp.Key))
                {
                    FmodSoundManager.ReleaseEventInstance(kvp.Value);
                    _entitiesToRemove.Add(kvp.Key);
                }
            }
            foreach (var entity in _entitiesToRemove)
            {
                _SoundElevationEventInstanceDictionary.Remove(entity);
            }
        }

        foreach ((RefRW<SFX_Elevation> sfx_Elevation, RefRO<LocalToWorld> localToWorld, Entity entity) in SystemAPI.Query<RefRW<SFX_Elevation>, RefRO<LocalToWorld>>().WithEntityAccess())
        {
            if (!_SoundElevationEventInstanceDictionary.ContainsKey(entity))
            {
                EventInstance eventInstance = FmodSoundManager.GetEventInstance(sfx_Elevation.ValueRO.soundEventReferenceGUID);
                FmodSoundManager.SetPositionEventInstance(eventInstance, localToWorld.ValueRO.Position);
                _SoundElevationEventInstanceDictionary.Add(entity, eventInstance);
            }
            else
            {
                EventInstance eventInstance = _SoundElevationEventInstanceDictionary[entity];

                // Update 3D position mỗi frame (turret có thể di chuyển)
                FmodSoundManager.SetPositionEventInstance(eventInstance, localToWorld.ValueRO.Position);

                if (sfx_Elevation.ValueRO.isPlaying)
                {
                    FmodSoundManager.SetParameterSoundEffectLoop(eventInstance, "SpeedElevationFactor", sfx_Elevation.ValueRO.elevationSpeedFactor);
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
        foreach (var kvp in _SoundElevationEventInstanceDictionary)
        {
            FmodSoundManager.ReleaseEventInstance(kvp.Value);
        }
        _SoundElevationEventInstanceDictionary.Clear();
    }
}
