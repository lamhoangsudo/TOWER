using FMOD.Studio;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
[UpdateAfter(typeof(TurretHeadingElevationSoundSystem))]
public partial class PlayAndStopSoundElevationSystem : SystemBase
{
    private Dictionary<Entity, EventInstance> _SoundElevationEventInstanceDictionary = new();
    [BurstCompile]
    protected override void OnCreate()
    {

    }

    protected override void OnUpdate()
    {
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

    [BurstCompile]
    protected override void OnDestroy()
    {

    }
}
