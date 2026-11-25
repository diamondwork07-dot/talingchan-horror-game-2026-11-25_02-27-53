using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const float MinXRotation = -90f;
    private const float MaxXRotation = 90f;
    private const float MinZRotation = -45f;
    private const float MaxZRotation = 45f;
    private const float FullCircleDegrees = 360f;

    [SerializeField] private Transform playerTransform;
    [SerializeField] private float mouseSensitivity = 0f; // TODO: Set balance value

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float zRotation = 0f;

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("CameraController: playerTransform is not assigned on " + gameObject.name + ". Camera rotation disabled.", this);
            enabled = false;
            return;
        }

        if (!transform.IsChildOf(playerTransform))
        {
            Debug.LogWarning("CameraController: " + gameObject.name + " is not a child of " + playerTransform.name + ". Pitch and roll will not follow the player correctly.", this);
        }

        if (mouseSensitivity < 0f)
        {
            Debug.LogWarning("CameraController: mouseSensitivity is negative on " + gameObject.name + ". Mouse input will be inverted.", this);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Mouse up (positive Y) looks up, which decreases the X angle.
        xRotation -= mouseY;

        // Mouse right (positive X) turns right, which increases the Y angle.
        yRotation += mouseX;

        xRotation = Mathf.Clamp(xRotation, MinXRotation, MaxXRotation);
        yRotation = Mathf.Repeat(yRotation, FullCircleDegrees);
        zRotation = Mathf.Clamp(zRotation, MinZRotation, MaxZRotation);

        playerTransform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, zRotation);
    }
}