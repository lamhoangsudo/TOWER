using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;     // tốc độ di chuyển
    [SerializeField] private float lookSpeed = 2f;      // tốc độ xoay chuột
    [SerializeField] private float sprintMultiplier = 2f; // tốc độ khi giữ Shift
    private float yaw;
    private float pitch;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Start()
    {

    }
    private void Update()
    {
        // --- Xoay camera bằng chuột ---
        Vector3 cameraDirection = math.normalizesafe(transform.position - Camera.main.transform.position);
        transform.rotation = math.slerp(transform.rotation, math.normalizesafe(Quaternion.LookRotation(math.normalizesafe(new Vector3(cameraDirection.x, 0, cameraDirection.z)))), Time.deltaTime * lookSpeed);

        // --- Di chuyển camera ---
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        Vector3 move = new Vector3(
            Input.GetAxis("Horizontal"), // A, D
            0,
            Input.GetAxis("Vertical")    // W, S
        );

        // Dùng transform để di chuyển theo hướng camera
        transform.Translate(move * speed * Time.deltaTime, Space.Self);

        // Di chuyển lên xuống bằng Q/E
        if (Input.GetKey(KeyCode.Q))
            transform.position += Vector3.down * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            transform.position += Vector3.up * speed * Time.deltaTime;
    }
    private void OnDestroy()
    {

    }
}
