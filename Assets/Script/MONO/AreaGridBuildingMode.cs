using System.Collections.Generic;
using UnityEngine;

public class AreaGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 startGridPosition;
    public Vector3 endGridPosition { get; private set; }
    private bool isDragging = false;
    public List<Vector3> gridPosition { get; private set; } = new List<Vector3>();
    public void OnEnd()
    {
        if(Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (gridPosition.Count != 0) gridPosition.Clear();
        }
    }

    public void OnStart()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            startPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            startGridPosition = GridManager.Instance.GetGridPosition(startPosition);
            if (gridPosition.Count != 0) gridPosition.Clear();
        }
    }

    public void OnUpdate()
    {
        if(Input.GetMouseButton(0) && isDragging)
        {
            if(gridPosition.Count != 0) gridPosition.Clear();
            endPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
            endGridPosition = GridManager.Instance.GetGridPosition(endPosition);
            if (endGridPosition.Equals(startGridPosition)) return;
            GetAreaGridBuild();
        }
    }

    private void GetAreaGridBuild()
    {
        Vector2Int size = BuildingManager.Instance.GetBuildingSize();
        float offsetx = endGridPosition.x - startGridPosition.x;
        float offsetz = endGridPosition.z - startGridPosition.z;
        for (int i = 0; i <= Mathf.Abs(offsetx); i++)
        {
            for (int j = 0; j <= Mathf.Abs(offsetz); j++)
            {
                gridPosition.Add(startGridPosition + new Vector3(i * Mathf.Sign(offsetx), 0, j * Mathf.Sign(offsetz)));
            }
        }
    }
}
