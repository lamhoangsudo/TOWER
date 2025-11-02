using Unity.Cinemachine;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using static Enum;
using static Enum.PlacementMode;
using static Enum.BuildingMode;
using static Enum.BuildRotationDirection;
using UnityEditor.Searcher;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private GhostBuildingManager ghostManager;
    [SerializeField] private Vector2Int buildingSize = new Vector2Int(1, 1);
    [SerializeField] private BuildRotationDirection buildRotationDirection = up;
    [SerializeField] private BuildRotationDirection currentBuildRotationDirection = up;

    private EntityQuery entityQuery;
    private EntityManager entityManager;
    private float buildDistance;
    private bool isBuildingMode = false;

    public IBuildingMode buildingMode { get; private set; }
    public PlacementMode placementMode { get; private set; } = PlacementMode.none;
    public BuildingMode buildingModeType { get; private set; } = BuildingMode.none;

    public Transform targetTranform { get; private set; }
    public Vector3 buildPosition { get; private set; }
    private Vector3 pointPosition;
    public bool isCanBuildable { get; private set; } = true;

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
        if (Input.GetKeyDown(KeyCode.R))
        {
            // TODO: Rotate Building Size
            if (placementMode == gridstyle)
            {
                SetBuildRotationDirection(buildRotationDirection);
            }
        }
        if (placementMode == freestyle)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                buildingModeType = area_free;
            }
            else
            {
                buildingModeType = single_free;
            }
        }
        else if (placementMode == gridstyle)
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
        SetPointBuildingVisual(buildPosition, buildingSize, pointPosition);
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
    private void SetPointBuildingVisual(Vector3 buildPosition, Vector2 scale, Vector3 pointPosition)
    {
        buildPosition -= CaculcateBuildindOffSetPositionWithRotation();
        ghostManager.SetPositionAndScale(buildPosition, scale, pointPosition, isCanBuildable);
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
                if (TryGetComponent<SingleGridBuildingMode>(out SingleGridBuildingMode singleGridBuildingMode))
                {
                    buildingMode = singleGridBuildingMode;
                    buildingMode.OnUpdate();
                    if (!singleGridBuildingMode.allGridContainIsValid || !singleGridBuildingMode.checkValidGrid)
                    {
                        isCanBuildable = false;
                    }
                    else
                    {
                        isCanBuildable = true;
                    }
                    if (isCanBuildable)
                    {
                        buildPosition = singleGridBuildingMode.buildPositionGridOrigin;
                        pointPosition = GridManager.Instance.GetVectorGridPosition(buildPosition);
                        buildPosition = pointPosition + GridManager.Instance.GetAdjustedPositionWithSizeBuilding(buildingSize);
                        buildRotationDirection = currentBuildRotationDirection;
                    }
                }
                break;
            case area_grid:
                if (TryGetComponent<AreaGridBuildingMode>(out AreaGridBuildingMode areaGridBuildingMode))
                {
                    buildingMode = areaGridBuildingMode;
                    buildingMode.OnStart();
                    buildingMode.OnUpdate();
                    buildingMode.OnEnd();
                    isCanBuildable = areaGridBuildingMode.allGridContainIsValid;
                    if (isCanBuildable)
                    {
                        buildPosition = areaGridBuildingMode.endGridOriginPosition;
                        pointPosition = GridManager.Instance.GetVectorGridPosition(buildPosition);
                        buildPosition = pointPosition + GridManager.Instance.GetAdjustedPositionWithSizeBuilding(buildingSize);
                        buildRotationDirection = currentBuildRotationDirection;
                    }
                }
                break;
            case line:
                break;
            case single_free:
                if (TryGetComponent<SingleFreeBuildingMode>(out SingleFreeBuildingMode singleFreeBuildingMode))
                {
                    buildingMode = singleFreeBuildingMode;
                    buildingMode.OnUpdate();
                    buildPosition = singleFreeBuildingMode.buildPosition;
                    pointPosition = buildPosition;
                }
                break;
        }
    }
    public Vector2Int GetBuildingSize()
    {
        return buildingSize;
    }
    public BuildRotationDirection GetCurrentBuildRotationDirection()
    {
        return currentBuildRotationDirection;
    }
    public float GetBuildRotationDirectionValue()
    {
        return (float)buildRotationDirection;
    }
    public void SetBuildRotationDirection(BuildRotationDirection buildRotationDirection)
    {
        switch (buildRotationDirection)
        {
            case BuildRotationDirection.up:
                currentBuildRotationDirection = BuildRotationDirection.right;
                break;
            case BuildRotationDirection.right:
                currentBuildRotationDirection = BuildRotationDirection.down;
                break;
            case BuildRotationDirection.down:
                currentBuildRotationDirection = BuildRotationDirection.left;
                break;
            case BuildRotationDirection.left:
                currentBuildRotationDirection = BuildRotationDirection.up;
                break;
        }
    }
    public void SetBuildRotationDirectionReverse(BuildRotationDirection buildRotationDirection)
    {
        switch (buildRotationDirection)
        {
            case BuildRotationDirection.up:
                this.buildRotationDirection = BuildRotationDirection.left;
                break;
            case BuildRotationDirection.right:
                this.buildRotationDirection = BuildRotationDirection.up;
                break;
            case BuildRotationDirection.down:
                this.buildRotationDirection = BuildRotationDirection.right;
                break;
            case BuildRotationDirection.left:
                this.buildRotationDirection = BuildRotationDirection.down;
                break;
        }
    }
    public Vector3 CaculcateBuildindOffSetPositionWithRotation()
    {
        float cell = GridManager.Instance.GetCellSize();
        float diameter = GridManager.Instance.GetDiameter();
        Vector3 offset = Vector3.zero;
        switch (buildRotationDirection)
        {
            case up:
                break;

            case down:
                offset = new Vector3((buildingSize.x - 1) * diameter, 0, (buildingSize.y - 1) * diameter);
                break;

            case left:
                offset = new Vector3((buildingSize.y + buildingSize.x - 2) * cell, 0, (buildingSize.y - buildingSize.x) * cell);
                break;

            case right:
                offset = new Vector3((buildingSize.x - buildingSize.y) * cell, 0, (buildingSize.x + buildingSize.y - 2) * cell);
                break;
        }
        return offset;
    }

}
