using System.Collections.Generic;
using UnityEngine;

public class SingleGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 buildPosition;
    public Vector3 buildPositionGridOrigin { get; private set; }
    public List<Vector3> listGridBuildingSizeContain { get; private set; } = new List<Vector3>();
    public void OnEnd()
    {
        
    }

    public void OnStart()
    {
        
    }

    public void OnUpdate()
    {
        buildPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
        buildPositionGridOrigin = GridManager.Instance.GetGridPosition(buildPosition);
        listGridBuildingSizeContain = GridManager.Instance.GetAllGridPosition(listGridBuildingSizeContain, BuildingManager.Instance.GetBuildingSize(), buildPositionGridOrigin);
    }
}
