using System.Collections.Generic;
using UnityEngine;
using static Enum;
using static Enum.PlacementMode;
using static Enum.BuildingMode;


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
            gridOrigin = GetVectorGridPosition(GetGridPosition(BuildingManager.Instance.targetTranform.position));
            transform.position = Vector3.Lerp(transform.position, gridOrigin, lerpSpeed * Time.deltaTime);
            buildingMode = BuildingManager.Instance.buildingMode;
            buildingModeEnum = BuildingManager.Instance.buildingModeType;
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
        if(Mathf.Abs(gridPosition.x) < gridSize && Mathf.Abs(gridPosition.z) < gridSize)
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
    public Vector3 GetGridPosition(Vector3 position)
    {
        position -= gridOrigin;
        float offsetx = Mathf.Sign(position.x) * cellSize;
        float offsetz = Mathf.Sign(position.z) * cellSize;
        int x = (int)((position.x + offsetx) / (diameter));
        int z = (int)((position.z + offsetz) / (diameter));
        Vector3 gridPosition = new Vector3 { x = x, y = 0, z = z, };
        if (CheckValidGridPosition(gridPosition))
        {
            return gridPosition;
        }
        return Vector3.zero;
    }
    public void SetUpNodeData()
    {
        switch(buildingModeEnum)
        {
            case single_grid:
                SingleGridBuildingMode singleGridBuildingMode = buildingMode as SingleGridBuildingMode;
                Vector3 gridPosition = singleGridBuildingMode.buildPositionGrid;
                foreach (var node in nodes)
                {
                    if (node.Key == gridPosition)
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
                    if (areaGridBuildingMode.gridPosition.Contains(node.Key))
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
    public List<Vector3> GetAllGridPosition(List<Vector3> vectors, Vector2 buildingSize, Vector3 gridPosition)
    {
        if(vectors.Count != 0) vectors.Clear();
        return vectors;
    }
}
