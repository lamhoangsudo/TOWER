using System.Collections.Generic;
using UnityEngine;

/*public class AreaGridBuildingMode : MonoBehaviour, IBuildingMode
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
            startGridOriginPosition = GridManager.Instance.GetGridPosition(startPosition);
            endGridOriginPosition = startGridOriginPosition;
            listGridBuildingSizeContain = GridManager.Instance.GetAllGridPosition(listGridBuildingSizeContain, BuildingManager.Instance.GetBuildingSize(), startGridOriginPosition);
        }
    }

    public void OnUpdate()
    {
        if (Input.GetMouseButton(0) && isDragging)
        {
            if (gridPositions.Count != 0) gridPositions.Clear();
            endPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            //Vector3 endGridPos = GridManager.Instance.GetGridPosition(endPosition);
            //Vector3 check = endGridPos - endGridOriginPosition;
            if(size != BuildingManager.Instance.GetBuildingSize()) size = BuildingManager.Instance.GetBuildingSize();
            *//*if (check.x % size.x != 0 || check.z % size.y != 0)
            {
                return;
            }*//*
            //else
            //{
            endGridOriginPosition = GridManager.Instance.GetGridPosition(endPosition);
            GetAreaGridBuild();
            //}
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
                if(checkAllGridAdd)
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
}*/
public class AreaGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 startGridOriginPosition;
    public Vector3 endGridOriginPosition { get; private set; }

    private bool isDragging = false;
    private Vector2Int size;
    private int multiplierX;
    private int multiplierZ;

    public List<Vector3> gridPositions { get; private set; } = new();
    public List<Vector3> listGridBuildingSizeContain { get; private set; } = new();

    public void OnStart()
    {
        // Khi nhấn chuột trái: bắt đầu vùng chọn
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;

            startPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            startGridOriginPosition = GridManager.Instance.GetGridPosition(startPosition);
            endGridOriginPosition = startGridOriginPosition;

            listGridBuildingSizeContain.Clear();
            listGridBuildingSizeContain = GridManager.Instance.GetAllGridPosition(
                listGridBuildingSizeContain,
                BuildingManager.Instance.GetBuildingSize(),
                startGridOriginPosition
            );

            gridPositions.Clear();
        }
    }

    public void OnUpdate()
    {
        if (!isDragging) return;

        // Khi đang kéo chuột: cập nhật vùng chọn
        if (Input.GetMouseButton(0))
        {
            endPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            endGridOriginPosition = GridManager.Instance.GetGridPosition(endPosition);

            size = BuildingManager.Instance.GetBuildingSize();
            GetAreaGridBuild();
        }

        // Khi thả chuột: kết thúc
        if (Input.GetMouseButtonUp(0))
        {
            OnEnd();
        }
    }

    public void OnEnd()
    {
        if (!isDragging) return;

        isDragging = false;
        gridPositions.Clear();
        listGridBuildingSizeContain.Clear();
    }

    private void GetAreaGridBuild()
    {
        gridPositions.Clear();

        multiplierX = (int)((endGridOriginPosition.x - startGridOriginPosition.x) / size.x);
        multiplierZ = (int)((endGridOriginPosition.z - startGridOriginPosition.z) / size.y);

        int stepX = (int)Mathf.Sign(multiplierX);
        int stepZ = (int)Mathf.Sign(multiplierZ);

        for (int i = 0; i <= Mathf.Abs(multiplierX); i++)
        {
            for (int j = 0; j <= Mathf.Abs(multiplierZ); j++)
            {
                bool allValid = true;

                foreach (Vector3 gridPos in listGridBuildingSizeContain)
                {
                    Vector3 candidate = gridPos + new Vector3(i * size.x * stepX, 0, j * size.y * stepZ);
                    if (!GridManager.Instance.CheckValidGridPosition(candidate))
                    {
                        allValid = false;
                        break;
                    }
                }

                if (allValid)
                {
                    foreach (Vector3 gridPos in listGridBuildingSizeContain)
                    {
                        Vector3 candidate = gridPos + new Vector3(i * size.x * stepX, 0, j * size.y * stepZ);
                        gridPositions.Add(candidate);
                    }
                }
            }
        }
    }
}

