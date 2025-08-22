using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Ray = UnityEngine.Ray;

public partial class MouseWorldPositionTrackSystem : SystemBase
{
    private Entity mouseWorldPositionTrackEntity;
    private Entity playerEntity;
    private CollisionWorld collisionWorld;
    protected override void OnCreate()
    {
        base.OnCreate();
    }
    protected override void OnStartRunning()
    {
        mouseWorldPositionTrackEntity = SystemAPI.GetSingletonEntity<MouseWorldPositionTrack>();
        playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
    }
    protected override void OnUpdate()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastInput raycastInput = new()
        {
            Start = mouseCameraRay.origin,
            End = mouseCameraRay.origin + mouseCameraRay.direction * 10f,
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
            localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, closestHit.Position, SystemAPI.Time.DeltaTime);
        }
        else
        {
            LocalTransform localTransformPlayer = SystemAPI.GetComponent<LocalTransform>(playerEntity);
            localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, localTransformPlayer.Position + localTransformPlayer.Forward() * 10f, SystemAPI.Time.DeltaTime);
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
