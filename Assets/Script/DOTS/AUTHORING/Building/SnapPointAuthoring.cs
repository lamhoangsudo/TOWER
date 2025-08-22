using Unity.Entities;
using UnityEngine;

public class SnapPointAuthoring : MonoBehaviour
{
    public Enum.SnapPointType snapPointType;
    public class SnapPointAuthoringBaker : Baker<SnapPointAuthoring>
    {
        public override void Bake(SnapPointAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SnapPoint
            {
                snapType = authoring.snapPointType,
                IsOccupied = false,
            });
        }
    }
}
public struct SnapPoint : IComponentData
{
    public Enum.SnapPointType snapType;
    public bool IsOccupied;
}


