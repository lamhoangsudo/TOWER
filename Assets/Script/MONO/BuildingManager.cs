using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using static Enum;
using static Enum.BuidingState;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Transform pointVisual;
    [SerializeField] private GhostBuildingManager ghostVisual;

    private EntityQuery entityQuery;
    private EntityManager entityManager;
    private float buildDistance;
    private bool isBuildingMode = false;

    public BuidingState buidingState { get; private set; } = none;
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
                buidingState = freestyle;
            } 
            else
            {
                buidingState = none;
            }
        }
        if (!isBuildingMode) return;
        if (Input.GetKeyDown(KeyCode.G))
        {
            buidingState = gridstyle;
        }
        if(Input.GetKeyDown(KeyCode.F))
        {
            buidingState = freestyle;
        }
        SetBuildingMode();
    }
    private void SetBuildingMode()
    {
        UnityEngine.Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        buildPosition = TrackBuildPosition(mouseCameraRay);
        pointVisual.transform.position = buildPosition;
        switch (buidingState)
        {
            case none:
                break;
            case freestyle:
                ghostVisual.transform.position = buildPosition;
                break;
            case gridstyle:
                ghostVisual.transform.position = GridManager.Instance.GetVectorGridPosition(GridManager.Instance.GetGridPosition(buildPosition));
                break;
        }
    }
    private Vector3 TrackBuildPosition(UnityEngine.Ray mouseCameraRay)
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
}
