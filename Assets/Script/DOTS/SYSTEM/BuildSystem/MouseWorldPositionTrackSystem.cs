using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using Ray = UnityEngine.Ray;

public partial class MouseWorldPositionTrackSystem : SystemBase
{
    private Entity mouseWorldPositionTrackEntity;
    private MouseWorldPositionTrack mouseWorldPositionTrack;
    private Entity playerEntity;
    private CollisionWorld collisionWorld;
    protected override void OnCreate()
    {
        base.OnCreate();
    }
    protected override void OnStartRunning()
    {
        mouseWorldPositionTrackEntity = SystemAPI.GetSingletonEntity<MouseWorldPositionTrack>();
        mouseWorldPositionTrack = SystemAPI.GetComponent<MouseWorldPositionTrack>(mouseWorldPositionTrackEntity);
        playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
    }
    protected override void OnUpdate()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(EventSystem.current.IsPointerOverGameObject()) return;
        RaycastInput raycastInput = new()
        {
            Start = mouseCameraRay.origin,
            End = mouseCameraRay.origin + mouseCameraRay.direction * mouseWorldPositionTrack.range,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 7 | 1u << 8,
                GroupIndex = 0,
            }
        };
        collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        RefRW<LocalTransform> localTransform = SystemAPI.GetComponentRW<LocalTransform>(mouseWorldPositionTrackEntity);
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit closestHit))
        {
            localTransform.ValueRW.Position = closestHit.Position;
        }
        else
        {
            LocalTransform localTransformPlayer = SystemAPI.GetComponent<LocalTransform>(playerEntity);
            localTransform.ValueRW.Position = localTransformPlayer.Position + localTransformPlayer.Forward() * mouseWorldPositionTrack.range;
        }
    }
    protected override void OnStopRunning()
    {
        base.OnStopRunning();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
