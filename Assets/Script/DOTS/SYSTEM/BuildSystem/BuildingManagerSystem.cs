using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct BuildingManagerSystem : ISystem
{
    private Entity mousePointEntity;
    private Entity buildingManagerEntity;
    private Entity storageEntity;
    private BuildingBuffer buildingBuffer;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitySpawnStorageTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        mousePointEntity = SystemAPI.GetSingletonEntity<MouseWorldPositionTrack>();
        buildingManagerEntity = SystemAPI.GetSingletonEntity<BuildingManger>();
        storageEntity = SystemAPI.GetSingletonEntity<EntitySpawnStorageTag>();
        MouseWorldPositionTrack mouseWorldPositionTrack = SystemAPI.GetComponent<MouseWorldPositionTrack>(mousePointEntity);
        BuildingManger buildingManager = SystemAPI.GetComponent<BuildingManger>(buildingManagerEntity);
        float3 buildPosition = float3.zero;

        DynamicBuffer<BuildingBuffer> _BuildingBuffer = SystemAPI.GetBuffer<BuildingBuffer>(storageEntity);
        for (int i = 0; i < _BuildingBuffer.Length; i++)
        {
            if (buildingManager.buildingID == _BuildingBuffer[i].buildingID)
            {
                buildingBuffer = _BuildingBuffer[i];
                break;
            }
        }
        if (mouseWorldPositionTrack.ghostEntity == Entity.Null)
        {
            //create
            mouseWorldPositionTrack.ghostEntity = state.EntityManager.Instantiate(buildingBuffer.entityGhostBuilding);
            SystemAPI.SetComponent<MouseWorldPositionTrack>(mousePointEntity, mouseWorldPositionTrack);
        }
        else
        {
            if (mouseWorldPositionTrack.ghostEntity != buildingBuffer.entityGhostBuilding)
            {
                //destroy and replace
                state.EntityManager.DestroyEntity(mouseWorldPositionTrack.ghostEntity);
                mouseWorldPositionTrack.ghostEntity = state.EntityManager.Instantiate(buildingBuffer.entityGhostBuilding);
                SystemAPI.SetComponent<MouseWorldPositionTrack>(mousePointEntity, mouseWorldPositionTrack);
            }
        }
        LocalTransform localTranformMousePointEntity = SystemAPI.GetComponent<LocalTransform>(mousePointEntity);
        buildPosition = localTranformMousePointEntity.Position;
        RefRW<LocalTransform> localTransformGhostEntity = SystemAPI.GetComponentRW<LocalTransform>(mouseWorldPositionTrack.ghostEntity);
        if(!localTransformGhostEntity.ValueRO.Position.Equals(buildPosition)) localTransformGhostEntity.ValueRW.Position = buildPosition;
        if(localTransformGhostEntity.ValueRO.Scale != 1f) localTransformGhostEntity.ValueRW.Scale = 1f;
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        CollisionFilter collisionFilter = new()
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << 8,
            GroupIndex = 0,
        };
        PointDistanceInput pointDistanceInput = new()
        {
            Filter = collisionFilter,
            MaxDistance = buildingBuffer.snapMaxDistance,
            Position = buildPosition,
        };
        bool hit = collisionWorld.CalculateDistance(pointDistanceInput, out DistanceHit closestHit);
        if (hit)
        {
            if (!SystemAPI.IsComponentEnabled<BuildingTrackMousePosition>(closestHit.Entity))
            {
                SystemAPI.SetComponentEnabled<BuildingTrackMousePosition>(closestHit.Entity, true);
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
