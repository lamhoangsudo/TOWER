using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingManager : MonoBehaviour
{
    private bool isBuildingMode = false;
    private EntityManager entityManager;
    private EntityQuery entityQuery;
    private ModelBuildingSo currentModelBuilding;
    [SerializeField] private float buildDistance;
    [SerializeField] private ListModelBuildingSo listModelBuilding;
    [SerializeField] private Enum.BuildingID currentBuildingID = Enum.BuildingID.None;
    [SerializeField] private Transform transformDebug;
    private Transform playerTranform;
    private CinemachineCamera cinemachineCamera;
    private GridBuildingManager gridBuildingManager;
    private void Start()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        playerTranform = cinemachineCamera.Target.TrackingTarget;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.B))
        {
            SetBuildingMode();
        }
        if(isBuildingMode)
        {
            // Implement building logic here
            UnityEngine.Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            GetPlayerPositionAndBuildingPosition(mouseCameraRay);
            if(Input.GetKeyDown(KeyCode.C))
            {
                int currentIndex = UnityEngine.Random.Range(0, listModelBuilding.list.Count);
                currentModelBuilding = listModelBuilding.list[currentIndex];
                currentBuildingID = currentModelBuilding.buildingID;
            }
            if (Input.GetMouseButtonDown(0))
            {
                entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
                entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<GridBuildingManager, LocalTransform>().Build(entityManager);
                LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entityQuery.GetSingletonEntity());
                float3 rayhitGridPositionDirection = gridBuildingManager.buildingPosition - localTransform.Position;
                float3 buildingPositionGrid = WorldPositionToGridPosition(rayhitGridPositionDirection, gridBuildingManager.cellSize);
                Debug.Log("Place Building at Grid Position at position" + buildingPositionGrid.ToString());
                // Example: Raycast to find ground position and place building
            }
        }
    }
    private void SetBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        entityQuery = entityManager.CreateEntityQuery(typeof(GridBuildingManager));
        gridBuildingManager = entityQuery.GetSingleton<GridBuildingManager>();
        gridBuildingManager.isBuildingMode = isBuildingMode;
        entityManager.SetComponentData(entityQuery.GetSingletonEntity(), gridBuildingManager);
        Debug.Log("Building Mode: " + (isBuildingMode ? "Enabled" : "Disabled"));
    }
    private void GetPlayerPositionAndBuildingPosition(UnityEngine.Ray mouseCameraRay)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        float3 buildPostion = TrackBuildPosition(mouseCameraRay);
        transformDebug.position = buildPostion;
        float3 playerPosition = playerTranform.position;
        entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<GridBuildingManager>().Build(entityManager);
        gridBuildingManager = entityQuery.GetSingleton<GridBuildingManager>();
        gridBuildingManager.playerPosition = playerPosition;
        gridBuildingManager.buildingPosition = buildPostion;
        entityManager.SetComponentData(entityQuery.GetSingletonEntity(), gridBuildingManager);

    }
    private float3 TrackBuildPosition(UnityEngine.Ray mouseCameraRay)
    {
        RaycastInput raycastInput = new()
        {
            Start = mouseCameraRay.origin,
            End = mouseCameraRay.origin + mouseCameraRay.direction * 1000f,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 7 | 1u << 8,
                GroupIndex = 0,
            }
        };
        PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit closestHit))
        {
            return closestHit.Position;
        }
        else
        {
            return transform.position + mouseCameraRay.direction * buildDistance;
        }
    }
    private float3 WorldPositionToGridPosition(float3 worldPosition, float cellSize)
    {
        GridBuildingSystem.GridPosition gridPosition = GridBuildingSystem.GetGridPosition(worldPosition, cellSize);
        return new float3(gridPosition.x, 0, gridPosition.z);
    }
}
