using Unity.Entities;

public struct SnapPointBuffer : IBufferElementData
{
    public Entity snapPointEntity;
    public float distanceSnapPointToBuildingGhost;
    public float offset;
}
