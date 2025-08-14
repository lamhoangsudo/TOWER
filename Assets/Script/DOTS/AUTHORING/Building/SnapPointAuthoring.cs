using Unity.Entities;
using UnityEngine;

public class SnapPointAuthoring : MonoBehaviour
{
    public Enum.SnapPointType snapPointType;
    public Enum.Direction direction;
    public class SnapPointAuthoringBaker : Baker<SnapPointAuthoring>
    {
        public override void Bake(SnapPointAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SnapPoint
            {
                snapType = authoring.snapPointType,
                IsOccupied = false,
                direction = authoring.direction,
            });
        }
    }
}
public struct SnapPoint : IComponentData
{
    public Enum.SnapPointType snapType;
    public bool IsOccupied;
    public Enum.Direction direction;
}


