using System.Collections.Generic;
using UnityEngine;

public class AreaGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 startGridOriginPosition;
    public Vector3 endGridOriginPosition { get; private set; }
    private bool isDragging = false;
    public List<Vector3> gridPositions { get; private set; } = new();
    public List<Vector3> listGridBuildingSizeContain { get; private set; } = new();
    private int multiplierx;
    private int multiplierz;
    private Vector2Int size;
    private Vector3 currentBuildPositionGrid;
    public bool allGridContainIsValid { get; private set; }
    public bool checkValidGrid { get; private set; }
    public void OnEnd()
    {
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (gridPositions.Count != 0) gridPositions.Clear();
            if (listGridBuildingSizeContain.Count != 0) listGridBuildingSizeContain.Clear();
        }
    }

    public void OnStart()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            if (gridPositions.Count != 0) gridPositions.Clear();
        }
        if (!(Input.GetMouseButton(0) && isDragging))
        {
            startPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            currentBuildPositionGrid = GridManager.Instance.GetGridPosition(startPosition, out bool checkCurrentBuildPositionGrid);
            checkValidGrid = checkCurrentBuildPositionGrid;
            if (currentBuildPositionGrid != startGridOriginPosition && checkValidGrid)
            {
                startGridOriginPosition = currentBuildPositionGrid;
                endGridOriginPosition = startGridOriginPosition;
                listGridBuildingSizeContain = GridManager.Instance.GetAllGridPosition(
                    listGridBuildingSizeContain,
                    BuildingManager.Instance.GetBuildingSize(),
                    startGridOriginPosition,
                    BuildingManager.Instance.GetCurrentBuildRotationDirection(),
                    out bool checkListCurrentGridBuildingSizeContain
                    );
                allGridContainIsValid = checkListCurrentGridBuildingSizeContain;
            }
        }
    }

    public void OnUpdate()
    {
        if (Input.GetMouseButton(0) && isDragging)
        {
            if (gridPositions.Count != 0) gridPositions.Clear();
            endPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            currentBuildPositionGrid = GridManager.Instance.GetGridPosition(endPosition, out bool checkCurrentBuildPositionGrid);
            checkValidGrid = checkCurrentBuildPositionGrid;
            if (currentBuildPositionGrid != endGridOriginPosition && checkValidGrid)
            {
                Vector3 endGridPos = currentBuildPositionGrid;
                Vector3 checkVector = endGridPos - endGridOriginPosition;
                if (size != BuildingManager.Instance.GetBuildingSize()) size = BuildingManager.Instance.GetBuildingSize();
                if (checkVector.x % size.x != 0 || checkVector.z % size.y != 0)
                {
                    return;
                }
                //else
                //{
                endGridOriginPosition = GridManager.Instance.GetGridPosition(endPosition, out bool checkEndGridOriginPosition);
                checkValidGrid = checkEndGridOriginPosition;
                GetAreaGridBuild();
                //}
            }
        }
    }

    private void GetAreaGridBuild()
    {
        multiplierx = (int)(endGridOriginPosition.x - startGridOriginPosition.x) / size.x;
        multiplierz = (int)(endGridOriginPosition.z - startGridOriginPosition.z) / size.y;
        for (int i = 0; i <= Mathf.Abs(multiplierx); i++)
        {
            for (int j = 0; j <= Mathf.Abs(multiplierz); j++)
            {
                bool checkAllGridAdd = true;
                foreach (Vector3 gridPos in listGridBuildingSizeContain)
                {
                    Vector3 gridAdd = gridPos + new Vector3((i * size.x) * Mathf.Sign(multiplierx), 0, j * size.y * Mathf.Sign(multiplierz));
                    if (!(GridManager.Instance.CheckValidGridPosition(gridAdd)))
                    {
                        checkAllGridAdd = false;
                        break;
                    }
                }
                if (checkAllGridAdd)
                {
                    foreach (Vector3 gridPos in listGridBuildingSizeContain)
                    {
                        Vector3 gridAdd = gridPos + new Vector3((i * size.x) * Mathf.Sign(multiplierx), 0, j * size.y * Mathf.Sign(multiplierz));
                        gridPositions.Add(gridAdd);
                    }
                }
            }
        }
    }

    public void OnInstantiate()
    {
        throw new System.NotImplementedException();
    }
}