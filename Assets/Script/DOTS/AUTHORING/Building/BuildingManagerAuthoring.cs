using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingManagerAuthoring : MonoBehaviour
{
    public Enum.BuildingID BuildingID;
    public class BuildingManagerAuthoringBaker : Baker<BuildingManagerAuthoring>
    {
        public override void Bake(BuildingManagerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildingManger()
            {
                buildingID = authoring.BuildingID,
            });
        }
    }
}
public struct BuildingManger : IComponentData
{
    public Enum.BuildingID buildingID;
    public float3 buildPosition;
}


