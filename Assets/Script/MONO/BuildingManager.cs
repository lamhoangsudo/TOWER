using Unity.Cinemachine;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using static Enum;
using static Enum.PlacementMode;
using static Enum.BuildingMode;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private GhostBuildingManager ghostManager;

    private EntityQuery entityQuery;
    private EntityManager entityManager;
    private float buildDistance;
    private bool isBuildingMode = false;

    public IBuildingMode buildingMode { get; private set; }
    public PlacementMode placementMode { get; private set; } = PlacementMode.none;
    public BuildingMode buildingModeType { get; private set; } = BuildingMode.none;

    public Transform targetTranform { get; private set; }
    public Vector3 buildPosition { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    private void Start()
    {
        targetTranform = cinemachineCamera.Target.TrackingTarget;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            isBuildingMode = !isBuildingMode;
            if (isBuildingMode)
            {
                placementMode = freestyle;
                buildingModeType = single_free;
            }
            else
            {
                placementMode = PlacementMode.none;
                buildingModeType = BuildingMode.none;
            }
        }
        if (!isBuildingMode) return;
        if (Input.GetKeyDown(KeyCode.G))
        {
            placementMode = gridstyle;
            buildingModeType = single_grid;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            placementMode = freestyle;
            buildingModeType = single_free;
        }
        if(placementMode == freestyle)
        {
            if(Input.GetKey(KeyCode.LeftShift))
            {
                buildingModeType = area_free;
            }
            else
            {
                buildingModeType = single_free;
            }
        }
        else if(placementMode == gridstyle)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                buildingModeType = area_grid;
            }
            else
            {
                buildingModeType = single_grid;
            }
        }
        SetPlacementMode();
        SetBuildMode();
        SetPointBuildingVisual(buildPosition);
    }
    private void SetPlacementMode()
    {
        switch (placementMode)
        {
            case PlacementMode.none:
                break;
            case freestyle:
                break;
            case gridstyle:
                break;
            default:
                placementMode = PlacementMode.none;
                break;
        }
    }
    private void SetPointBuildingVisual(Vector3 buildPosition)
    {
        ghostManager.SetPosition(buildPosition);
    }
    public Vector3 TrackBuildPosition(UnityEngine.Ray mouseCameraRay)
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
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit closestHit))
        {
            return closestHit.Position;
        }
        else
        {
            return cinemachineCamera.transform.position + mouseCameraRay.direction * buildDistance;
        }
    }
    private void SetBuildMode()
    {
        switch (buildingModeType)
        {
            case BuildingMode.none:
                break;
            case single_grid:
                if(TryGetComponent<SingleGridBuildingMode>(out SingleGridBuildingMode singleGridBuildingMode))
                {
                    buildingMode = singleGridBuildingMode;
                    buildingMode.OnUpdate();
                    buildPosition = singleGridBuildingMode.buildPositionGrid;
                    buildPosition = GridManager.Instance.GetVectorGridPosition(buildPosition);
                }
                break;
            case area_grid:
                if(TryGetComponent<AreaGridBuildingMode>(out AreaGridBuildingMode areaGridBuildingMode))
                {
                    buildingMode = areaGridBuildingMode;
                    buildingMode.OnStart();
                    buildingMode.OnUpdate();
                    buildingMode.OnEnd();
                    buildPosition = areaGridBuildingMode.endGridPosition;
                    buildPosition = GridManager.Instance.GetVectorGridPosition(buildPosition);
                }
                break;
            case line:
                break;
            case single_free:
                if(TryGetComponent<SingleFreeBuildingMode>(out SingleFreeBuildingMode singleFreeBuildingMode))
                {
                    buildingMode = singleFreeBuildingMode;
                    buildingMode.OnUpdate();
                    buildPosition = singleFreeBuildingMode.buildPosition;
                }
                break;
            //TODO: Remove Area Free Building Mode 
            case area_free:
                if(TryGetComponent<AreaFreeBuildingMode>(out AreaFreeBuildingMode areaFreeBuildingMode))
                {
                    buildingMode = areaFreeBuildingMode;
                    buildingMode.OnStart();
                    buildingMode.OnUpdate();
                    buildingMode.OnEnd();
                }
                break;
        }
    }
}
