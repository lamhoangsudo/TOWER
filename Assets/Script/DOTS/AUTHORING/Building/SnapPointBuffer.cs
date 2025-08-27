using Unity.Entities;
using Unity.Mathematics;
using static Enum;

public struct SnapPointBuffer : IBufferElementData
{
    public Entity snapPointEntity;
    public float3 snapPointPosition;
    public SnapPointType snapPointType;
    public bool isOccupied;
    public float distanceSnapPointToBuildingGhost;
    public float offset;
}
public struct SnapPointsDirectionBuffer : IBufferElementData
{
    public Entity SnapPointsDirectionEntity;
    public Enum.Direction direction;
    public float3 directionVector;
}