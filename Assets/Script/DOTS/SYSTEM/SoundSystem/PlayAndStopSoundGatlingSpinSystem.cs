using FMOD.Studio;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(BarrelFireEffectSystem))]
public partial class PlayAndStopSoundGatlingSpinSystem : SystemBase
{
    private Dictionary<Entity, EventInstance> _SoundGatlingSpinEventInstanceDictionary = new();
    private List<Entity> _entitiesToRemove = new();

    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        // Cleanup: release instances cho entities đã bị destroy
        if (_SoundGatlingSpinEventInstanceDictionary.Count > 0)
        {
            _entitiesToRemove.Clear();
            foreach (var kvp in _SoundGatlingSpinEventInstanceDictionary)
            {
                if (!EntityManager.Exists(kvp.Key))
                {
                    FmodSoundManager.ReleaseEventInstance(kvp.Value);
                    _entitiesToRemove.Add(kvp.Key);
                }
            }
            foreach (var entity in _entitiesToRemove)
            {
                _SoundGatlingSpinEventInstanceDictionary.Remove(entity);
            }
        }

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

                // Update 3D position mỗi frame (turret có thể di chuyển)
                FmodSoundManager.SetPositionEventInstance(eventInstance, localToWorld.ValueRO.Position);

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

    protected override void OnDestroy()
    {
        // Release tất cả FMOD instances khi system bị destroy
        foreach (var kvp in _SoundGatlingSpinEventInstanceDictionary)
        {
            FmodSoundManager.ReleaseEventInstance(kvp.Value);
        }
        _SoundGatlingSpinEventInstanceDictionary.Clear();
    }
}
