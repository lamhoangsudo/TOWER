using Unity.Entities;

public struct SnapPointBuffer : IBufferElementData
{
    public Entity snapPointEntity;
    public float distanceSnapPointToBuildingGhost;
    public float offset;
}
public struct SnapPointsDirectionBuffer : IBufferElementData
{
    public Entity SnapPointsDirectionEntity;
    public Enum.Direction direction;
}