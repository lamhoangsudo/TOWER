using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GridBuildingManagerAuthoring : MonoBehaviour
{
    public float cellSize;
    public int gridSize;
    public class GridBuildingManagerAuthoringBaker : Baker<GridBuildingManagerAuthoring>
    {
        public override void Bake(GridBuildingManagerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GridBuildingManager()
            {
                cellSize = authoring.cellSize,
                gridSize = authoring.gridSize
            });
        }
    }
}
public struct GridBuildingManager : IComponentData
{
    public float cellSize;
    public int gridSize;
    public float3 playerPosition;
    public float3 buildingPosition;
    public bool isBuildingMode;
}


