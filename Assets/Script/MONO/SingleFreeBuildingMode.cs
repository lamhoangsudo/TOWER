using UnityEngine;

public class SingleFreeBuildingMode : MonoBehaviour, IBuildingMode
{
    public Vector3 buildPosition { get; private set; }
    public void OnEnd()
    {

    }

    public void OnInstantiate()
    {
        throw new System.NotImplementedException();
    }

    public void OnStart()
    {

    }

    public void OnUpdate()
    {
        buildPosition = BuildingManager.Instance.TrackBuildPosition(Camera.main.ScreenPointToRay(Input.mousePosition));
    }
}
