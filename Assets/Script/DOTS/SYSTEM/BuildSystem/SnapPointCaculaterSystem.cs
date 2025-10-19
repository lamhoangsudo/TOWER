using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
//[UpdateAfter(typeof(BuildingManagerSystem))]
//[UpdateAfter(typeof(SnapPointCheckAvaliableSystem))]
public partial struct SnapPointCaculaterSystem : ISystem
{
    private Entity mousePointEntity;
    private NativeArray<float3> faceNormals;
    private Enum.Direction snapDirection;
    private Enum.Direction snapGhostDirection;
    private float3 position;
    private float3 forward;
    private float offset;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //faceNormals = new(6, Allocator.Persistent);
        //faceNormals[0] = math.up();
        //faceNormals[1] = math.down();
        //faceNormals[2] = math.left();
        //faceNormals[3] = math.right();
        //faceNormals[4] = math.forward();
        //faceNormals[5] = math.back();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //mousePointEntity = SystemAPI.GetSingletonEntity<BuildPositionTag>();
        //BuildPositionTag mouseWorldPositionTrack = SystemAPI.GetComponent<BuildPositionTag>(mousePointEntity);
        //BuildingManger buildingManger = SystemAPI.GetSingleton<BuildingManger>();
        //float3 gridPosition = SystemAPI.GetComponent<LocalTransform>(mousePointEntity).Position;
        //foreach ((RefRO<LocalTransform> buildingLocalTransform,
        //    RefRO<Building> building,
        //    EnabledRefRW<BuildingTrackMousePosition> buildingTrackMousePositionEnabled,
        //    RefRW<BuildingTrackMousePosition> buildingTrackMousePosition,
        //    DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers)
        //    in
        //    SystemAPI.Query<RefRO<LocalTransform>,
        //    RefRO<Building>,
        //    EnabledRefRW<BuildingTrackMousePosition>,
        //    RefRW<BuildingTrackMousePosition>,
        //    DynamicBuffer<SnapPointsDirectionBuffer>>())
        //{
        //    if (math.distance(buildingLocalTransform.ValueRO.Position, gridPosition) > building.ValueRO.snapMaxDistance || mouseWorldPositionTrack.ghostEntity == Entity.Null)
        //    {
        //        if (buildingTrackMousePositionEnabled.ValueRO) buildingTrackMousePositionEnabled.ValueRW = false;
        //        snapDirection = Enum.Direction.none;
        //        snapGhostDirection = Enum.Direction.none;
        //        continue;
        //    }
        //    buildingTrackMousePosition.ValueRW.gridPosition = gridPosition;
        //    float3 worldBuildingAndGhostDirection = math.normalizesafe(buildingTrackMousePosition.ValueRO.gridPosition - buildingLocalTransform.ValueRO.Position);
        //    RefRW<LocalTransform> ghostLocalTranform = SystemAPI.GetComponentRW<LocalTransform>(mouseWorldPositionTrack.ghostEntity);

        //    buildingTrackMousePosition.ValueRW.snapDirection = CaculatorSnapDirection(ref state, buildingLocalTransform.ValueRO.Rotation, worldBuildingAndGhostDirection);
        //    buildingTrackMousePosition.ValueRW.snapGhostDirection = CaculatorSnapDirection(ref state, ghostLocalTranform.ValueRO.Rotation, worldBuildingAndGhostDirection);
        //    if (snapDirection != buildingTrackMousePosition.ValueRW.snapDirection)
        //    {
        //        snapDirection = buildingTrackMousePosition.ValueRW.snapDirection;
        //        for (int i = 0; i < snapPointsDirectionBuffers.Length; i++)
        //        {
        //            if (snapPointsDirectionBuffers[i].direction == snapDirection)
        //            {
        //                Entity snapPointDirectionBuildingEntity = snapPointsDirectionBuffers[i].SnapPointsDirectionEntity;
        //                DynamicBuffer<SnapPointBuffer> snapPointBuildingBuffers = SystemAPI.GetBuffer<SnapPointBuffer>(snapPointDirectionBuildingEntity);
        //                //find snap point with min distance to building
        //                UpdateAllDistanceSnapPointToBuildingGhost(ref state, snapPointBuildingBuffers, gridPosition);
        //                position = GetSuitableSnapPointBuilding(ref state, snapPointBuildingBuffers, buildingManger.snapPointTypeSearch);
        //                forward = snapPointsDirectionBuffers[i].directionVector;
        //                break;
        //            }
        //        }
        //    }
        //    if (snapGhostDirection != buildingTrackMousePosition.ValueRW.snapGhostDirection && snapDirection != Enum.Direction.none)
        //    {
        //        DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionGhostBuildingBuffers = SystemAPI.GetBuffer<SnapPointsDirectionBuffer>(mouseWorldPositionTrack.ghostEntity);
        //        snapGhostDirection = buildingTrackMousePosition.ValueRW.snapGhostDirection;
        //        for (int i = 0; i < snapPointsDirectionGhostBuildingBuffers.Length; i++)
        //        {
        //            if (snapPointsDirectionGhostBuildingBuffers[i].direction == snapGhostDirection)
        //            {
        //                Entity snapPointDirectionGhostBuilding = snapPointsDirectionGhostBuildingBuffers[i].SnapPointsDirectionEntity;
        //                DynamicBuffer<SnapPointBuffer> snapPointGhostBuildingBuffers = SystemAPI.GetBuffer<SnapPointBuffer>(snapPointDirectionGhostBuilding);
        //                //find snap point with min distance to building
        //                offset = snapPointGhostBuildingBuffers[0].offset;
        //                break;
        //            }
        //        }
        //    }
        //    quaternion targetQuaternion = quaternion.identity;
        //    if (buildingTrackMousePosition.ValueRO.snapGhostDirection == Enum.Direction.up || buildingTrackMousePosition.ValueRO.snapGhostDirection == Enum.Direction.down)
        //    {
        //        targetQuaternion = buildingLocalTransform.ValueRO.Rotation;
        //    }
        //    else if (buildingTrackMousePosition.ValueRO.snapGhostDirection != Enum.Direction.none)
        //    {
        //        targetQuaternion = math.mul(
        //            CacuLatorTargetQuanternion(
        //                ref state,
        //                buildingTrackMousePosition.ValueRO.snapGhostDirection,
        //                ghostLocalTranform.ValueRO.Rotation,
        //                buildingTrackMousePosition.ValueRO.snapDirection,
        //                buildingLocalTransform.ValueRO.Rotation
        //                ),
        //            ghostLocalTranform.ValueRO.Rotation);
        //    }

        //    if (!buildingTrackMousePosition.ValueRO.targetQuaternion.Equals(targetQuaternion))
        //    {
        //        buildingTrackMousePosition.ValueRW.targetQuaternion = targetQuaternion;
        //    }

        //    ghostLocalTranform.ValueRW.Rotation = buildingTrackMousePosition.ValueRO.targetQuaternion;
        //    if (position.Equals(float3.zero) || forward.Equals(float3.zero))
        //    {
        //        ghostLocalTranform.ValueRW.Position = gridPosition;
        //    }
        //    else
        //    {
        //        ghostLocalTranform.ValueRW.Position = position + forward * offset;
        //    }
        //}
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        faceNormals.Dispose();
    }
    public void UpdateAllDistanceSnapPointToBuildingGhost(ref SystemState state, DynamicBuffer<SnapPointBuffer> snapPointBuffers, float3 buildPosition)
    {
        //DynamicBuffer<SnapPointBuffer> __newsnapPointBuffers__ = snapPointBuffers;
        //for (int i = 0; i < snapPointBuffers.Length; i++)
        //{
        //    SnapPointBuffer snapPointBuffer = snapPointBuffers[i];
        //    LocalToWorld localToWorld = SystemAPI.GetComponent<LocalToWorld>(snapPointBuffer.snapPointEntity);
        //    snapPointBuffer.distanceSnapPointToBuildingGhost = math.distance(localToWorld.Position, gridPosition);
        //    __newsnapPointBuffers__[i] = snapPointBuffer;
        //}
    }
    public float3 GetClosetDirection(ref SystemState state, float3 localDir)
    {
        int nearestFace = -1;
        float bestDot = -2f;
        for (int i = 0; i < faceNormals.Length; i++)
        {
            float d = math.dot(localDir, faceNormals[i]);
            if (d > bestDot)
            {
                bestDot = d;
                nearestFace = i;
            }
        }
        return faceNormals[nearestFace];
    }
    public Enum.Direction GetEnumDirectionFromDirection(ref SystemState state, float3 localDir)
    {
        if (localDir.Equals(math.up()))
        {
            return Enum.Direction.up;
        }
        else if (localDir.Equals(math.down()))
        {
            return Enum.Direction.down;
        }
        else if (localDir.Equals(math.left()))
        {
            return Enum.Direction.left;
        }
        else if (localDir.Equals(math.right()))
        {
            return Enum.Direction.right;
        }
        else if (localDir.Equals(math.forward()))
        {
            return Enum.Direction.forward;
        }
        else if (localDir.Equals(math.back()))
        {
            return Enum.Direction.backward;
        }
        else
        {
            return Enum.Direction.none;
        }
    }
    public float3 GetDirectionFromEnumDirection(Enum.Direction direction)
    {
        switch (direction)
        {
            case Enum.Direction.up:
                return math.up();
            case Enum.Direction.down:
                return math.down();
            case Enum.Direction.left:
                return math.left();
            case Enum.Direction.right:
                return math.right();
            case Enum.Direction.forward:
                return math.forward();
            case Enum.Direction.backward:
                return math.back();
        }
        return float3.zero;
    }
    public quaternion FromToRotation(float3 from, float3 to)
    {
        from = math.normalize(from);
        to = math.normalize(to);
        float dot = math.dot(from, to);
        if (dot > 0.9999f) return quaternion.identity;
        float3 axis;
        if (dot < -0.9999f)
        {
            axis = math.cross(new float3(1, 0, 0), from);
            if (math.lengthsq(axis) < 1e-6f)
                axis = math.cross(new float3(0, 1, 0), from);
            axis = math.normalize(axis);
            return quaternion.AxisAngle(axis, math.PI);
        }
        axis = math.normalize(math.cross(from, to));
        float angle = math.acos(dot);
        return quaternion.AxisAngle(axis, angle);
    }
    public float3 GetWorldDirection(float3 direction, quaternion quaternion)
    {
        return math.mul(quaternion, direction);
    }
    public Enum.Direction CaculatorSnapDirection(ref SystemState state, quaternion worldRotation, float3 worldBuildingAndGhostDirection)
    {
        quaternion localQuaternion = math.inverse(worldRotation);
        float3 localDirection = math.mul(localQuaternion, worldBuildingAndGhostDirection);
        float3 closetDirection = GetClosetDirection(ref state, localDirection);
        return GetEnumDirectionFromDirection(ref state, closetDirection);
    }
    public quaternion CacuLatorTargetQuanternion(ref SystemState state, Enum.Direction direction1, quaternion quaternion1, Enum.Direction direction2, quaternion quaternion2)
    {
        float3 directionf1 = GetDirectionFromEnumDirection(direction1);
        float3 directionf2 = GetDirectionFromEnumDirection(direction2);
        float3 directionfw1 = GetWorldDirection(directionf1, quaternion1);
        float3 directionfw2 = GetWorldDirection(directionf2, quaternion2);
        return FromToRotation(directionfw1, directionfw2);
    }
    public float3 GetSuitableSnapPointBuilding(ref SystemState state, DynamicBuffer<SnapPointBuffer> snapPointBuffers, Enum.SnapPointType snapPointTypeSearch)
    {
        float3 position = float3.zero;
        float minDistance = float.MaxValue;
        for (int i = 0; i < snapPointBuffers.Length; i++)
        {
            SnapPointBuffer snapPointBuffer = snapPointBuffers[i];
            if (snapPointBuffer.snapPointType == snapPointTypeSearch && !snapPointBuffer.isOccupied)
            {
                if (snapPointBuffer.distanceSnapPointToBuildingGhost < minDistance)
                {
                    minDistance = snapPointBuffer.distanceSnapPointToBuildingGhost;
                    position = snapPointBuffer.snapPointPosition;
                }
            }
        }
        return position;
    }
    public void UpdateSnapPointdistanceToBuildingGhost(ref SystemState state, DynamicBuffer<SnapPointBuffer> snapPointBuffers, float3 buildPosition)
    {
        for (int i = 0; i < snapPointBuffers.Length; i++)
        {
            if(snapPointBuffers[i].isOccupied) continue;
            SnapPointBuffer snapPointBuffer = snapPointBuffers[i];
            snapPointBuffer.distanceSnapPointToBuildingGhost = math.distance(snapPointBuffer.snapPointPosition, buildPosition);
            snapPointBuffers[i] = snapPointBuffer;
        }
    }
}
