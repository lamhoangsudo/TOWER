using System.Collections.Generic;
using UnityEngine;

public class SingleGridBuildingMode : MonoBehaviour, IBuildingMode
{
    private Vector3 buildPosition;
    public Vector3 buildPositionGrid { get; private set; }
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
        buildPositionGrid = GridManager.Instance.GetGridPosition(buildPosition);
    }
}
