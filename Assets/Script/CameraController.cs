using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const float MinPitch = -90f;
    private const float MaxPitch = 90f;
    private const float MinYaw = -90f;
    private const float MaxYaw = 90f;
    private const float MinRoll = -45f;
    private const float MaxRoll = 45f;

    [SerializeField] private float pitchSensitivity = 0f; // TODO: Set balance value
    [SerializeField] private float yawSensitivity = 0f;   // TODO: Set balance value

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float zRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pitchSensitivity < 0f)
        {
            Debug.LogWarning("CameraController: pitchSensitivity is negative on " + gameObject.name + ". Pitch input will be inverted.", this);
        }

        if (yawSensitivity < 0f)
        {
            Debug.LogWarning("CameraController: yawSensitivity is negative on " + gameObject.name + ". Yaw input will be inverted.", this);
        }
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        // Mouse up (positive Y) looks up, which decreases X rotation.
        xRotation -= mouseY * pitchSensitivity;

        // Mouse right (positive X) turns right, which increases Y rotation.
        yRotation += mouseX * yawSensitivity;

        xRotation = Mathf.Clamp(xRotation, MinPitch, MaxPitch);
        yRotation = Mathf.Clamp(yRotation, MinYaw, MaxYaw);
        zRotation = Mathf.Clamp(zRotation, MinRoll, MaxRoll);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
    }
}