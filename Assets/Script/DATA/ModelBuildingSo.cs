using UnityEngine;

[CreateAssetMenu(fileName = "ModelBuildingSo", menuName = "Scriptable Objects/ModelBuildingSo")]
public class ModelBuildingSo : ScriptableObject
{
    public GameObject buildingGhostPrefab;
    public GameObject buildingPrefab;
    public Enum.BuildingID buildingID;
    public float snapMaxDistance;
    public float timeBuildMax;
    public Enum.SnapPointType snapPointTypeSearch;
}
