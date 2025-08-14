using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
[UpdateAfter(typeof(BuildingManagerSystem))]
public partial struct BuildingTrackBuildPositionSystem : ISystem
{
    private Entity mousePointEntity;
    private NativeArray<float3> faceNormals;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        faceNormals = new(6, Allocator.Persistent);
        faceNormals[0] = math.up();
        faceNormals[1] = math.down();
        faceNormals[2] = math.left();
        faceNormals[3] = math.right();
        faceNormals[4] = math.forward();
        faceNormals[5] = math.back();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        mousePointEntity = SystemAPI.GetSingletonEntity<MouseWorldPositionTrack>();
        MouseWorldPositionTrack mouseWorldPositionTrack = SystemAPI.GetComponent<MouseWorldPositionTrack>(mousePointEntity);
        float3 buildPosition = SystemAPI.GetComponent<LocalTransform>(mousePointEntity).Position;
        foreach ((RefRO<LocalTransform> buildingLocalTransform,
            RefRO<Building> building,
            EnabledRefRW<BuildingTrackMousePosition> buildingTrackMousePositionEnabled,
            RefRW<BuildingTrackMousePosition> buildingTrackMousePosition,
            DynamicBuffer<SnapPointBuffer> snapPointBuffers)
            in
            SystemAPI.Query<RefRO<LocalTransform>,
            RefRO<Building>,
            EnabledRefRW<BuildingTrackMousePosition>,
            RefRW<BuildingTrackMousePosition>,
            DynamicBuffer<SnapPointBuffer>>())
        {
            if (math.distance(buildingLocalTransform.ValueRO.Position, buildPosition) > building.ValueRO.snapMaxDistance)
            {
                if (buildingTrackMousePositionEnabled.ValueRO) buildingTrackMousePositionEnabled.ValueRW = false;
                UpdateAllDistanceSnapPointToBuildingGhost(ref state, snapPointBuffers, float.MaxValue);
                continue;
            }
            buildingTrackMousePosition.ValueRW.buildPosition = buildPosition;
            UpdateAllDistanceSnapPointToBuildingGhost(ref state, snapPointBuffers, buildingTrackMousePosition.ValueRO.buildPosition);
            float3 worldDirection = math.normalizesafe(buildingTrackMousePosition.ValueRO.buildPosition - buildingLocalTransform.ValueRO.Position);
            float3 localBuidlingDirection = math.mul(math.inverse(buildingLocalTransform.ValueRO.Rotation), worldDirection);
            buildingTrackMousePosition.ValueRW.snapDirection = GetEnumDirectionFromDirection(ref state, GetClosetDirection(ref state, localBuidlingDirection));
            RefRW<LocalTransform> ghostLocalTranform = SystemAPI.GetComponentRW<LocalTransform>(mouseWorldPositionTrack.ghostEntity);
            float3 localGhostBuidlingDirection = math.mul(math.inverse(ghostLocalTranform.ValueRO.Rotation), worldDirection);
            buildingTrackMousePosition.ValueRW.SnapGhostDirection = GetEnumDirectionFromDirection(ref state, GetClosetDirection(ref state, localGhostBuidlingDirection));
            quaternion targetQuaternion = math.mul(FromToRotation(GetWorldDirection(GetDirectionFromEnumDirection(buildingTrackMousePosition.ValueRW.SnapGhostDirection), ghostLocalTranform.ValueRO.Rotation), GetWorldDirection(GetDirectionFromEnumDirection(buildingTrackMousePosition.ValueRW.snapDirection), buildingLocalTransform.ValueRO.Rotation)), ghostLocalTranform.ValueRO.Rotation);
            if (!buildingTrackMousePosition.ValueRO.targetQuaternion.Equals(targetQuaternion))
            {
                buildingTrackMousePosition.ValueRW.targetQuaternion = targetQuaternion;
            }
            ghostLocalTranform.ValueRW.Rotation = buildingTrackMousePosition.ValueRO.targetQuaternion;
            for (int i = 0; i < snapPointBuffers.Length; i++)
            {
                SnapPoint snapPoint = SystemAPI.GetComponent<SnapPoint>(snapPointBuffers[i].snapPointEntity);
                if (snapPoint.direction != buildingTrackMousePosition.ValueRO.snapDirection) continue;
                DynamicBuffer<SnapPointBuffer> snapPointBuffersGhost = SystemAPI.GetBuffer<SnapPointBuffer>(mouseWorldPositionTrack.ghostEntity);
                for (int j = 0; j < snapPointBuffersGhost.Length; j++)
                {
                    SnapPoint snapPointGhost = SystemAPI.GetComponent<SnapPoint>(snapPointBuffersGhost[j].snapPointEntity);
                    if (snapPointGhost.direction != buildingTrackMousePosition.ValueRO.SnapGhostDirection) continue;
                    LocalToWorld localToWorldSnapPointBuilding = SystemAPI.GetComponent<LocalToWorld>(snapPointBuffers[i].snapPointEntity);
                    ghostLocalTranform.ValueRW.Position = localToWorldSnapPointBuilding.Position + localToWorldSnapPointBuilding.Forward * snapPointBuffersGhost[j].offset;
                    return;
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        faceNormals.Dispose();
    }
    public void UpdateAllDistanceSnapPointToBuildingGhost(ref SystemState state, DynamicBuffer<SnapPointBuffer> snapPointBuffers, float3 buildPosition)
    {
        DynamicBuffer<SnapPointBuffer> __newsnapPointBuffers__ = snapPointBuffers;
        for (int i = 0; i < snapPointBuffers.Length; i++)
        {
            SnapPointBuffer snapPointBuffer = snapPointBuffers[i];
            LocalToWorld localToWorld = SystemAPI.GetComponent<LocalToWorld>(snapPointBuffer.snapPointEntity);
            snapPointBuffer.distanceSnapPointToBuildingGhost = math.distance(localToWorld.Position, buildPosition);
            __newsnapPointBuffers__[i] = snapPointBuffer;
        }
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
}
