using Unity.Cinemachine;
using UnityEngine;

public class CameraOrbitController : MonoBehaviour
{
    [Range(1, 500)]
    [SerializeField] private float rotateSpeed;
    [Range(1, 5)]
    [SerializeField] private float zoomedSpeed;
    private CinemachineOrbitalFollow orbitalFollow;
    private float yaw;
    private float pitch;
    private void Awake()
    {
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
    }
    private void Start()
    {
        
    }
    private void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            yaw = Mathf.Clamp(yaw, orbitalFollow.HorizontalAxis.Range.x, orbitalFollow.HorizontalAxis.Range.y);
            pitch = Mathf.Clamp(pitch, orbitalFollow.VerticalAxis.Range.x, orbitalFollow.VerticalAxis.Range.y);
            orbitalFollow.HorizontalAxis.Value = yaw;
            orbitalFollow.VerticalAxis.Value = pitch;
            if(Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
            {
                orbitalFollow.RadialAxis.Value += Input.mouseScrollDelta.y * zoomedSpeed * Time.deltaTime;
                orbitalFollow.RadialAxis.Value = Mathf.Clamp(orbitalFollow.RadialAxis.Value, orbitalFollow.RadialAxis.Range.x, orbitalFollow.RadialAxis.Range.y);
            }
        }
        if(Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
