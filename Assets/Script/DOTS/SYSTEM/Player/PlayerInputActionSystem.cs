using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class PlayerInputActionSystem : SystemBase
{
    private Entity playerEntity;
    private PlayerInputAction inputActions;
    private Entity mousePointEntity;
    private EntityManager entityManager;
    protected override void OnCreate()
    {
        inputActions = new PlayerInputAction();
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    protected override void OnStartRunning()
    {
        inputActions.Enable();
        playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
    }

    protected override void OnUpdate()
    {
        float3 moveInput = (float3) inputActions.Player.Move.ReadValue<Vector3>();
        Movement movement = SystemAPI.GetComponent<Movement>(playerEntity);
        float3 curretMoveInput = movement.moveVector;
        if (!moveInput.Equals(curretMoveInput))
        {
            float moveSpeed = movement.moveSpeed;
            SystemAPI.SetComponent<Movement>(playerEntity, new Movement()
            {
                moveVector = (float3)moveInput,
                moveSpeed = movement.moveSpeed,
            });
        }
        if (Input.GetMouseButton(1))
        {
            Rotation rotation = SystemAPI.GetComponent<Rotation>(playerEntity);
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            SystemAPI.SetComponent<Rotation>(playerEntity, new Rotation()
            {
                rotationSpeed = rotation.rotationSpeed,
                yaw = rotation.yaw + mouseX * rotation.rotationSpeed,
                pitch = rotation.pitch - mouseY * rotation.rotationSpeed,
                pitchMax = rotation.pitchMax,
                pitchMin = rotation.pitchMin,
            });
        }
        if (Input.GetMouseButtonDown(0))
        {
            mousePointEntity = SystemAPI.GetSingletonEntity<MouseWorldPositionTrack>();
            MouseWorldPositionTrack mouseWorldPositionTrack = SystemAPI.GetComponent<MouseWorldPositionTrack>(mousePointEntity);
            if (mouseWorldPositionTrack.ghostEntity != Entity.Null)
            {
                LocalToWorld localToWorldTarget = SystemAPI.GetComponent<LocalToWorld>(mouseWorldPositionTrack.ghostEntity);
                Entity building = entityManager.Instantiate(mouseWorldPositionTrack.ghostEntity);
                LocalTransform localTransform = SystemAPI.GetComponent<LocalTransform>(building);
                entityManager.SetComponentData<LocalTransform>(building, new LocalTransform
                {
                    Position = localToWorldTarget.Position,
                    Rotation = localToWorldTarget.Rotation,
                    Scale = 1f,
                });
                RefRW<BuildingGhost> buildingGhost = SystemAPI.GetComponentRW<BuildingGhost>(building);
                SystemAPI.SetComponentEnabled<IsBuilding>(building, true);
            }
        }
        if(Input.GetKeyDown(KeyCode.R))
        {

        }
    }
    protected override void OnStopRunning()
    {
        inputActions.Disable();
        playerEntity = Entity.Null;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
