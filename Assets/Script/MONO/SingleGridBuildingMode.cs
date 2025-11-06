using System;
using System.Collections.Generic;
using UnityEngine;

public class SingleGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 buildPosition;
    public Vector3 buildPositionGridOrigin { get; private set; }
    private Vector3 currentBuildPositionGrid;
    public List<Vector3> listGridBuildingSizeContain { get; private set; } = new();
    private List<Vector3> listCurrentGridBuildingSizeContain = new();
    public bool allGridContainIsValid { get; private set; }
    public bool checkValidGrid { get; private set; }

    public static event EventHandler OnPlaceBuilding;

    public void OnEnd()
    {

    }

    public void OnStart()
    {

    }

    public void OnUpdate()
    {
        buildPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
        currentBuildPositionGrid = GridManager.Instance.GetGridPosition(buildPosition, out bool checkCurrentBuildPositionGrid);
        checkValidGrid = checkCurrentBuildPositionGrid;
        if (checkValidGrid)
        {
            buildPositionGridOrigin = currentBuildPositionGrid;
            listCurrentGridBuildingSizeContain = GridManager.Instance.GetAllGridPosition(
                listCurrentGridBuildingSizeContain,
                BuildingManager.Instance.currentBuildingConfigDataBlob.buildingSizeCell,
                buildPositionGridOrigin, BuildingManager.Instance.GetCurrentBuildRotationDirection(),
                out bool checkListCurrentGridBuildingSizeContain
                );
            allGridContainIsValid = checkListCurrentGridBuildingSizeContain;
            if (allGridContainIsValid)
            {
                listGridBuildingSizeContain = listCurrentGridBuildingSizeContain;
            }
            if (Input.GetMouseButtonDown(0))
            {
                OnInstantiate();
            }
        }
    }

    public void OnInstantiate()
    {
        //TODO: Place Building
        OnPlaceBuilding?.Invoke(this, EventArgs.Empty);
        Debug.Log("place building");
    }
}
