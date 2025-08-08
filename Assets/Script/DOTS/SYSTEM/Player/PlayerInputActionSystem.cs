using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class PlayerInputActionSystem : SystemBase
{
    private Entity playerEntity;
    private PlayerInputAction inputActions;
    protected override void OnCreate()
    {
        inputActions = new PlayerInputAction();
    }
    protected override void OnStartRunning()
    {
        inputActions.Enable();
        playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
    }
    protected override void OnUpdate()
    {
        float2 moveInput = (float2) inputActions.Player.Move.ReadValue<Vector2>();
        Movement movement = SystemAPI.GetComponent<Movement>(playerEntity);
        float2 curretMoveInput = movement.moveVector;
        if(!moveInput.Equals(curretMoveInput))
        {
            float moveSpeed = movement.moveSpeed;
            SystemAPI.SetComponent<Movement>(playerEntity, new Movement()
            {
                moveVector = (float2) moveInput,
                moveSpeed = movement.moveSpeed,
            });
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
