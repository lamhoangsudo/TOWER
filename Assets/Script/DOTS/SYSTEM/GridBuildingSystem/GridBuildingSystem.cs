using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.UI.Image;

public partial struct GridBuildingSystem : ISystem
{
    public struct GridPosition
    {
        public int x;
        public int z;
        public int y;
        public string ToString => $"({x}, {y}, {z})";
    }
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach((RefRW<GridBuildingManager> gridBuildingManager, RefRW<LocalTransform> localTranform) in SystemAPI.Query<RefRW<GridBuildingManager>, RefRW<LocalTransform>>())
        {
            if (!gridBuildingManager.ValueRO.isBuildingMode) return;
            GridPosition gridPositionpPlayer = GetGridPosition(gridBuildingManager.ValueRO.playerPosition, gridBuildingManager.ValueRO.cellSize);
            localTranform.ValueRW.Position = new float3(gridPositionpPlayer.x * gridBuildingManager.ValueRO.cellSize * 2, 0, gridPositionpPlayer.z * gridBuildingManager.ValueRO.cellSize * 2);
            DebugGrid(gridBuildingManager.ValueRO.cellSize, gridBuildingManager.ValueRO.gridSize, localTranform.ValueRO.Position);
            GridPosition gridPositionBuild = GetGridPosition(gridBuildingManager.ValueRO.buildingPosition - localTranform.ValueRO.Position, gridBuildingManager.ValueRO.cellSize);
        }
    }

    private void DebugGrid(float cellSize, int gridSize, float3 buildingOrginGrid)
    {
        buildingOrginGrid = buildingOrginGrid - new float3(cellSize, 0, cellSize);
        float totalWidth = gridSize * cellSize * 2;
        float totalDepth = gridSize * cellSize * 2;

        // Dọc theo trục Z++
        for (int x = 0; x <= gridSize; x++)
        {
            float3 start = buildingOrginGrid + new float3(x * cellSize * 2, 0, 0);
            float3 end = start + new float3(0, 0, totalDepth);
            UnityEngine.Debug.DrawLine(start, end, UnityEngine.Color.green);
            float3 end1 = start - new float3(0, 0, totalDepth);
            UnityEngine.Debug.DrawLine(start, end1, UnityEngine.Color.green);
        }

        // Ngang theo trục X++
        for (int z = 0; z <= gridSize; z++)
        {
            float3 start = buildingOrginGrid + new float3(0, 0, z * cellSize * 2);
            float3 end = start + new float3(totalWidth, 0, 0);
            UnityEngine.Debug.DrawLine(start, end, UnityEngine.Color.green);
            float3 end1 = start - new float3(totalWidth, 0, 0);
            UnityEngine.Debug.DrawLine(start, end1, UnityEngine.Color.green);
        }

        // Dọc theo trục Z--
        for (int x = 0; x >= -gridSize; x--)
        {
            float3 start = buildingOrginGrid + new float3(x * cellSize * 2, 0, 0);
            float3 end = start - new float3(0, 0, totalDepth);
            UnityEngine.Debug.DrawLine(start, end, UnityEngine.Color.green);
            float3 end1 = start + new float3(0, 0, totalDepth);
            UnityEngine.Debug.DrawLine(start, end1, UnityEngine.Color.green);
        }

        // Ngang theo trục X--
        for (int z = 0; z >= -gridSize; z--)
        {
            float3 start = buildingOrginGrid + new float3(0, 0, z * cellSize * 2);
            float3 end = start - new float3(totalWidth, 0, 0);
            UnityEngine.Debug.DrawLine(start, end, UnityEngine.Color.green);
            float3 end1 = start + new float3(totalWidth, 0, 0);
            UnityEngine.Debug.DrawLine(start, end1, UnityEngine.Color.green);
        }

        // Gốc tọa độ
        UnityEngine.Debug.DrawRay(buildingOrginGrid, math.up(), UnityEngine.Color.red);
    }
    //private GridPosition GetGridPosition(float3 position, float cellSize)
    //{
    //    int x = (int)math.floor(position.x / cellSize);
    //    int z = (int)math.floor(position.z / cellSize);
    //    return new GridPosition { x = x, z = z, y = 0 };
    //}
    public static GridPosition GetGridPosition(float3 position, float cellSize)
    {
        float offsetx = math.sign(position.x) * cellSize;
        float offsetz = math.sign(position.z) * cellSize;
        int x = (int)((position.x + offsetx) / (cellSize * 2));
        int z = (int)((position.z + offsetz) / (cellSize * 2));
        return new GridPosition { x = x, z = z, y = 0 };
    }
    private bool IsPositionOccupied(GridPosition gridPosition, int gridSize)
    {
        if(math.abs(gridPosition.x) > gridSize || math.abs(gridPosition.z) > gridSize)
        {
            return false;
        }
        return true;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

}
