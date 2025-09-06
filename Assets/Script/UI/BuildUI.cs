using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class BuildUI : MonoBehaviour
{
    [SerializeField] private Button changeBuilS;
    [SerializeField] private Button changeBuilL;
    private EntityManager entityManager;
    private void Start()
    {
        changeBuilS.onClick.AddListener(() =>
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            RefRW<BuildingManger> buildingManger = entityManager.CreateEntityQuery(typeof(BuildingManger)).GetSingletonRW<BuildingManger>();
            buildingManger.ValueRW.buildingID = Enum.BuildingID.Platform_S;
        });
        changeBuilL.onClick.AddListener(() =>
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            RefRW<BuildingManger> buildingManger = entityManager.CreateEntityQuery(typeof(BuildingManger)).GetSingletonRW<BuildingManger>();
            buildingManger.ValueRW.buildingID = Enum.BuildingID.Platform_L;
        });
    }
}
