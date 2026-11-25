using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 0f; // TODO: Set balance value

    private CharacterController characterController;

    private void Awake()
    {
        if (!TryGetComponent(out characterController))
        {
            Debug.LogError("PlayerMovement: CharacterController component is missing on " + gameObject.name, this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (characterController == null)
        {
            return;
        }

        if (moveSpeed < 0f)
        {
            Debug.LogWarning("PlayerMovement: moveSpeed is negative on " + gameObject.name + ". Movement will be reversed.", this);
        }

        float verticalInput = GetVerticalInput();
        float horizontalInput = GetHorizontalInput();

        Vector3 moveDirection = (transform.forward * verticalInput) + (transform.right * horizontalInput);
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 displacement = moveDirection * moveSpeed * Time.deltaTime;
        characterController.Move(displacement);
    }

    private float GetVerticalInput()
    {
        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool backwardPressed = Input.GetKey(KeyCode.S);

        if (forwardPressed && backwardPressed)
        {
            return 0f;
        }

        if (forwardPressed)
        {
            return 1f;
        }

        if (backwardPressed)
        {
            return -1f;
        }

        return 0f;
    }

    private float GetHorizontalInput()
    {
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool leftPressed = Input.GetKey(KeyCode.A);

        if (rightPressed && leftPressed)
        {
            return 0f;
        }

        if (rightPressed)
        {
            return 1f;
        }

        if (leftPressed)
        {
            return -1f;
        }

        return 0f;
    }
}