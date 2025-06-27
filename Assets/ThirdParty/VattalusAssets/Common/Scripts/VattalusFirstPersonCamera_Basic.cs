using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class VattalusFirstPersonCamera_Basic : MonoBehaviour
{
    //This script handles the player controls

    //CAMERA VARIABLES
    public Camera cameraComponent;

    [Tooltip("Use mouse scroll to change camera FOV. First value should be smaller than the second value")]
    public Vector2 camFovRange = new Vector2(15f, 60f);
    private float fovTarget = 60f;

    //Different player control types
    public enum ControlModeTypes
    {
        Walking,
        Flying
    }
    public ControlModeTypes controlMode;

    // horizontal and vertical rotation speeds
    public float cameraSensitivity = 90;
    private float rotationX = 0f;
    private float rotationY = 0f;

    //MOVEMENT VARIABLES
    CharacterController characterController;
    public float WalkSpeed = 1.5f;
    public float SprintSpeed = 3f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float Gravity = 9.8f;
    private float GravVelocity = 0;


    void Start()
    {
        Cursor.visible = false;

        //Automatically correct the movement speeds to the default values
        if (WalkSpeed <= 0f) WalkSpeed = 1f;
        if (WalkSpeed > SprintSpeed) SprintSpeed = WalkSpeed * 2f;

        characterController = GetComponent<CharacterController>();
        if (characterController == null) Debug.Log("color=#FF0000>VattalusAssets: [FirstPersonCamera] Missing CharacterController component. Add it this GameObject</color>");


        //initialize FOV and automatically correct the values
        if (camFovRange == null) camFovRange = new Vector2(15f, 60f);
        else
        {
            //cap fov range to reasonable values
            if (camFovRange.x < 2f) camFovRange.x = 2f;
            if (camFovRange.y < 20f) camFovRange.y = 20f;
            if (camFovRange.y > 120f) camFovRange.y = 120f;
            //make sure smaller value comes first
            if (camFovRange.x > camFovRange.y)
            {
                float tempFOV = camFovRange.y;
                camFovRange.y = camFovRange.x;
                camFovRange.x = tempFOV;
            }
        }

        fovTarget = camFovRange.y;
        if (cameraComponent != null) cameraComponent.fieldOfView = camFovRange.y;
    }

    void FixedUpdate()
    {
        /////////////////////////////////////////////////////////
        //CAMERA FOV
        fovTarget += -Input.mouseScrollDelta.y * 3f;
        fovTarget = Mathf.Clamp(fovTarget, camFovRange.x, camFovRange.y);
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, fovTarget, 10f * Time.deltaTime);

        /////////////////////////////////////////////////////////
        //CAMERA POSITION/ROTATION
        #region Camera Movement

        //read the mouse input
        rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;

        //restrict the camera angle
        rotationY = Mathf.Clamp(rotationY, -90, 90);


        //Now let's add the mouse inputs to the camera itself
        if (cameraComponent != null)
        {
            cameraComponent.transform.localRotation = Quaternion.AngleAxis(rotationY, Vector3.left);
        }
        #endregion

        /////////////////////////////////////////////////////////
        //PLAYER MOVEMENT
        #region Player Movement

        if (controlMode == ControlModeTypes.Walking || controlMode == ControlModeTypes.Flying)
        {
            //for walking / flying, apply the X axis (left/right) mouse movements to the entire character
            transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);

            // character movement
            float horizontal = Input.GetAxis("Horizontal") * (Input.GetKey(sprintKey) ? SprintSpeed : WalkSpeed);
            float vertical = Input.GetAxis("Vertical") * (Input.GetKey(sprintKey) ? SprintSpeed : WalkSpeed);
            characterController.Move((transform.right * horizontal + transform.forward * vertical) * Time.deltaTime);

            if (controlMode == ControlModeTypes.Walking)
            {
                // when walking, apply gravity
                if (characterController.isGrounded)
                {
                    GravVelocity = 0;
                }
                else
                {
                    GravVelocity -= Gravity * Time.deltaTime;
                    characterController.Move(new Vector3(0, GravVelocity, 0));
                }
            }

            if (controlMode == ControlModeTypes.Flying)
            {
                //use Q and E keys to climb/descent while flying
                if (Input.GetKey(KeyCode.Q)) { transform.position -= transform.up * WalkSpeed * Time.deltaTime; }
                if (Input.GetKey(KeyCode.E)) { transform.position += transform.up * WalkSpeed * Time.deltaTime; }
            }
        }
        #endregion
    }

    void Update()
    {

    }

    //METHODS TO SET CONTROL MODE
    public void SetPlayerControl(ControlModeTypes newMode)
    {
        SetPlayerControl(newMode, null, new Vector2(180, 90));
        rotationX = transform.rotation.eulerAngles.y;
    }

    public void SetPlayerControl(ControlModeTypes newMode, [CanBeNull]Transform camAnchorRef, Vector2 angleConstraints)
    {
        controlMode = newMode;
    }
}
