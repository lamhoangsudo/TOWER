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
    private CollisionWorld collisionWorld;
    private Entity buildingManagerEntity;
    private Entity entityStorage;
    protected override void OnCreate()
    {
        base.OnCreate();
    }
    protected override void OnStartRunning()
    {
        mouseWorldPositionTrackEntity = SystemAPI.GetSingletonEntity<MouseWorldPositionTrack>();
        buildingManagerEntity = SystemAPI.GetSingletonEntity<BuildingManger>();
        entityStorage = SystemAPI.GetSingletonEntity<EntitySpawnStorageTag>();
    }
    protected override void OnUpdate()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastInput raycastInput = new()
        {
            Start = mouseCameraRay.origin,
            End = mouseCameraRay.origin + mouseCameraRay.direction * 1000f,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 7 | 1u << 8,
                GroupIndex = 0,
            }
        };
        collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit closestHit))
        {
            RefRW<LocalTransform> localTransform = SystemAPI.GetComponentRW<LocalTransform>(mouseWorldPositionTrackEntity);
            localTransform.ValueRW.Position = closestHit.Position;
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
