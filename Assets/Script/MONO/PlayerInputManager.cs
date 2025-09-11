using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    private PlayerInputAction inputActions;
    private EntityManager entityManager;
    public static PlayerInputManager Instance;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float pitchMin;
    [SerializeField] private float pitchMax;
    [SerializeField] private float pitch;
    [SerializeField] private float yaw;
    private void Awake()
    {
        inputActions = new PlayerInputAction();
        inputActions.Enable();
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    private void Update()
    {
        if (!inputActions.Player.Move.ReadValue<Vector3>().Equals(Vector3.zero))
        {
            transform.position += moveSpeed * Time.deltaTime * inputActions.Player.Move.ReadValue<Vector3>();
        }
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            yaw = rotationSpeed * Input.GetAxis("Mouse X") * Time.deltaTime;
            pitch = rotationSpeed * Input.GetAxis("Mouse Y") * Time.deltaTime;
            transform.Rotate(-pitch, yaw, 0);
        }
    }
    private void OnDestroy()
    {
        inputActions.Disable();
    }
}
