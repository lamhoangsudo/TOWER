using Unity.Entities;
using UnityEngine;
using static Enum;
public class SnapPointAuthoring : MonoBehaviour
{
    public SnapPointType snapPointType;
    public class SnapPointAuthoringBaker : Baker<SnapPointAuthoring>
    {
        public override void Bake(SnapPointAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SnapPoint()
            {
                snapPointType = authoring.snapPointType,
                isOccupied = false,
                distanceSnapPointToBuildingGhost= 0f,
                offset = 0f,
            });
        }
    }
}
public struct SnapPoint : IComponentData
{
    public SnapPointType snapPointType;
    public bool isOccupied;
    public float distanceSnapPointToBuildingGhost;
    public float offset;
}

