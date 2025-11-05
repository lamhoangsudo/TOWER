using System.Collections.Generic;
using UnityEngine;
using static Enum;
using static Enum.PlacementMode;
using static Enum.BuildingMode;
using static Enum.BuildRotationDirection;
using Unity.Mathematics;


public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int gridSize;
    [SerializeField] private float cellSize;
    [SerializeField] private GridBuildVisualManager gridBuildVisualManager;
    [SerializeField] private Vector3 gridOrigin;
    [SerializeField] private SingleGridVisual gridVisual;
    [SerializeField] private Transform gridContain;
    [Range(1f, 100f)]
    [SerializeField] private float lerpSpeed = 5f;
    private Dictionary<Vector3, SingleGridVisual> nodes = new();
    private Vector3 gridPosition;
    private float diameter;
    private IBuildingMode buildingMode;
    private BuildingMode buildingModeEnum;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    private void Start()
    {
        diameter = cellSize * 2;
        InstantiateGridVisual();
    }
    private void Update()
    {
        if (BuildingManager.Instance.placementMode == gridstyle)
        {
            ShowGrid();
            gridOrigin = GetVectorGridPosition(GetGridPosition(BuildingManager.Instance.targetTranform.position, out _));
            transform.position = Vector3.Lerp(transform.position, gridOrigin, lerpSpeed * Time.deltaTime);
            buildingMode = BuildingManager.Instance.buildingMode;
            buildingModeEnum = BuildingManager.Instance.buildingModeType;
            if (!BuildingManager.Instance.isCanBuildable) return;
            SetUpNodeData();
        }
        else
        {
            HideGrid();
        }
    }
    private void ShowGrid()
    {
        gridBuildVisualManager.gameObject.SetActive(true);
    }
    private void HideGrid()
    {
        gridBuildVisualManager.gameObject.SetActive(false);
    }
    public void InstantiateGridVisual()
    {
        for (int i = -(gridSize - 1); i < gridSize; i++)
        {
            for (int j = -(gridSize - 1); j < gridSize; j++)
            {
                SingleGridVisual visual = Instantiate(gridVisual, gridContain.position + new Vector3(diameter * i, 0, diameter * j), Quaternion.identity, gridContain);
                visual.transform.localScale = new Vector3(diameter, 1, diameter);
                nodes.Add(new Vector3 { x = i, y = 0, z = j, }, visual);
            }
        }
    }
    public bool CheckValidGridPosition(Vector3 gridPosition)
    {
        if (Mathf.Abs(gridPosition.x) < gridSize && Mathf.Abs(gridPosition.z) < gridSize)
        {
            return true;
        }
        return false;
    }
    public Vector3 GetVectorGridPosition(Vector3 gridPosition)
    {
        Vector3 position = new Vector3(diameter * gridPosition.x, 0, diameter * gridPosition.z) + gridOrigin;
        return position;
    }
    public Vector3 GetGridPosition(Vector3 position, out bool checkValidGrid)
    {
        position -= gridOrigin;
        float offsetx = Mathf.Sign(position.x) * cellSize;
        float offsetz = Mathf.Sign(position.z) * cellSize;
        int x = (int)((position.x + offsetx) / (diameter));
        int z = (int)((position.z + offsetz) / (diameter));
        Vector3 gridPosition = new Vector3 { x = x, y = 0, z = z, };
        if (CheckValidGridPosition(gridPosition))
        {
            checkValidGrid = true;
            return gridPosition;
        }
        checkValidGrid = false;
        return Vector3.zero;
    }
    public void SetUpNodeData()
    {
        switch (buildingModeEnum)
        {
            case single_grid:
                SingleGridBuildingMode singleGridBuildingMode = buildingMode as SingleGridBuildingMode;
                List<Vector3> listGridBuildingSizeContain = singleGridBuildingMode.listGridBuildingSizeContain;
                foreach (var node in nodes)
                {
                    if (listGridBuildingSizeContain.Contains(node.Key))
                    {
                        if (node.Value.pointStatus != Enum.PointBuidStatus.validPointBuid)
                            node.Value.pointStatus = Enum.PointBuidStatus.validPointBuid;
                    }
                    else
                    {
                        if (node.Value.pointStatus != Enum.PointBuidStatus.none)
                            node.Value.pointStatus = Enum.PointBuidStatus.none;
                    }
                }
                break;
            case area_grid:
                AreaGridBuildingMode areaGridBuildingMode = buildingMode as AreaGridBuildingMode;
                foreach (var node in nodes)
                {
                    if (areaGridBuildingMode.gridPositions.Contains(node.Key))
                    {
                        if (node.Value.pointStatus != Enum.PointBuidStatus.validPointBuid)
                            node.Value.pointStatus = Enum.PointBuidStatus.validPointBuid;
                    }
                    else
                    {
                        if (node.Value.pointStatus != Enum.PointBuidStatus.none)
                            node.Value.pointStatus = Enum.PointBuidStatus.none;
                    }
                }
                break;
        }
    }
    public Vector3 GetAdjustedPositionWithSizeBuilding(Vector2Int buildingSize)
    {
        if (((buildingSize.x & 1) == 1 && (buildingSize.y & 1) == 1))
        {
            return Vector3.zero;
        }
        float offsetX = buildingSize.x - 1;
        float offsetZ = buildingSize.y - 1;
        Vector3 adjustedGridPosition = new()
        {
            x = offsetX * cellSize,
            y = 0,
            z = offsetZ * cellSize,
        };
        return adjustedGridPosition;
    }
    public List<Vector3> GetAllGridPosition(List<Vector3> vectors, int3 buildingSize, Vector3 gridOrginPosition, BuildRotationDirection buildRotationDirection, out bool allGridContainIsValid)
    {
        if (vectors.Count != 0) vectors.Clear();
        allGridContainIsValid = true;
        if (!((buildingSize.x & 1) == 1 && (buildingSize.z & 1) == 1))
        {
            switch (buildRotationDirection)
            {
                case up:
                    for (int x = 0; x < buildingSize.x; x++)
                    {
                        for (int z = 0; z < buildingSize.z; z++)
                        {
                            Vector3 gridPosition = gridOrginPosition + new Vector3(x, 0, z);
                            if (CheckValidGridPosition(gridPosition))
                            {
                                vectors.Add(gridPosition);
                            }
                            else
                            {
                                allGridContainIsValid = false;
                            }
                        }
                    }
                    break;
                case down:
                    for (int x = 0; x < buildingSize.x; x++)
                    {
                        for (int z = 0; z < buildingSize.z; z++)
                        {
                            Vector3 gridPosition = gridOrginPosition + new Vector3(-x, 0, -z);
                            if (CheckValidGridPosition(gridPosition))
                            {
                                vectors.Add(gridPosition);
                            }
                            else
                            {
                                allGridContainIsValid = false;
                            }
                        }
                    }
                    break;
                case left:
                    for (int x = 0; x < buildingSize.x; x++)
                    {
                        for (int z = 0; z < buildingSize.z; z++)
                        {
                            Vector3 gridPosition = gridOrginPosition + new Vector3(-z, 0, x);
                            if (CheckValidGridPosition(gridPosition))
                            {
                                vectors.Add(gridPosition);
                            }
                            else
                            {
                                allGridContainIsValid = false;
                            }
                        }
                    }
                    break;
                case right:
                    for (int x = 0; x < buildingSize.x; x++)
                    {
                        for (int z = 0; z < buildingSize.z; z++)
                        {
                            Vector3 gridPosition = gridOrginPosition + new Vector3(z, 0, -x);
                            if (CheckValidGridPosition(gridPosition))
                            {
                                vectors.Add(gridPosition);
                            }
                            else
                            {
                                allGridContainIsValid = false;
                            }
                        }
                    }
                    break;
            }
        }
        else
        {
            float x_2 = buildingSize.x / 2f;
            float z_2 = buildingSize.z / 2f;
            for (int x = 0; x < x_2; x++)
            {
                if(!allGridContainIsValid)
                {
                    break;
                }
                for (int z = 0; z < z_2; z++)
                {
                    Vector3 gridPosition = gridOrginPosition + new Vector3(x, 0, z);
                    if (CheckValidGridPosition(gridPosition))
                    {
                        if (vectors.Contains(gridPosition)) continue;
                        vectors.Add(gridPosition);
                    }
                    else
                    {
                        allGridContainIsValid = false;
                        break;
                    }
                }
                for (int z = 0; z > -z_2; z--)
                {
                    Vector3 gridPosition = gridOrginPosition + new Vector3(x, 0, z);
                    if (CheckValidGridPosition(gridPosition))
                    {
                        if (vectors.Contains(gridPosition)) continue;
                        vectors.Add(gridPosition);
                    }
                    else
                    {
                        allGridContainIsValid = false;
                        break;
                    }
                }
            }
            for (int x = 0; x > -x_2; x--)
            {
                if (!allGridContainIsValid)
                {
                    break;
                }
                for (int z = 0; z < z_2; z++)
                {
                    Vector3 gridPosition = gridOrginPosition + new Vector3(x, 0, z);
                    if (CheckValidGridPosition(gridPosition))
                    {
                        if (vectors.Contains(gridPosition)) continue;
                        vectors.Add(gridPosition);
                    }
                    else
                    {
                        allGridContainIsValid = false;
                        break;
                    }
                }
                for (int z = 0; z > -z_2; z--)
                {
                    Vector3 gridPosition = gridOrginPosition + new Vector3(x, 0, z);
                    if (CheckValidGridPosition(gridPosition))
                    {
                        if (vectors.Contains(gridPosition)) continue;
                        vectors.Add(gridPosition);
                    }
                    else
                    {
                        allGridContainIsValid = false;
                        break;
                    }
                }
            }
        }
        return vectors;
    }
    public float GetCellSize()
    {
        return cellSize;
    }
    public float GetDiameter()
    {
        return diameter;
    }
}
