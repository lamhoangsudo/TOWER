using System.Collections.Generic;
using UnityEngine;

public class SingleGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 buildPosition;
    public Vector3 buildPositionGridOrigin { get; private set; }
    private Vector3 currentBuildPositionGrid;
    public List<Vector3> listGridBuildingSizeContain { get; private set; } = new List<Vector3>();
    private List<Vector3> listCurrentGridBuildingSizeContain = new List<Vector3>();
    //Debug
    public bool allGridContainIsValid;
    public bool checkValidGrid;
    public void OnEnd()
    {

    }

    public void OnStart()
    {

    }

    public void OnUpdate()
    {
        buildPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
        currentBuildPositionGrid = GridManager.Instance.GetGridPosition(buildPosition, out checkValidGrid);
        if (checkValidGrid)
        {
            buildPositionGridOrigin = currentBuildPositionGrid;
            listCurrentGridBuildingSizeContain = GridManager.Instance.GetAllGridPosition(
                listCurrentGridBuildingSizeContain,
                BuildingManager.Instance.GetBuildingSize(),
                buildPositionGridOrigin, BuildingManager.Instance.GetCurrentBuildRotationDirection(),
                out allGridContainIsValid
                );
            if(allGridContainIsValid)
            {
                listGridBuildingSizeContain = listCurrentGridBuildingSizeContain;
            }
        }
    }
}
