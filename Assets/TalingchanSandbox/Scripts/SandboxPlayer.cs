using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Talingchan.Sandbox
{
    [RequireComponent(typeof(CharacterController))]
    public class SandboxPlayer : MonoBehaviour
    {
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float jumpHeight = 1.15f;
        public float mouseSensitivity = 0.12f;
        public string hudTitle = "TALINGCHAN";
        public Transform view;
        public Transform avatar;
        public bool thirdPerson = true;
        public bool allowViewSwitch = true;
        public float cameraDistance = 3.4f;
        CharacterController body;
        SandboxPhone phone;
        Vector3 spawn;
        float pitch, vertical;
        bool jumpHeld, resetHeld, escapeHeld, viewHeld;
        int ignoreLookFrames;
        public int simulatedFrames { get; private set; }

        void Start()
        {
            body = GetComponent<CharacterController>();
            phone = GetComponent<SandboxPhone>();
            spawn = transform.position;
            Application.runInBackground = true;
            pitch = thirdPerson ? 12 : 0;
            LockMouse(true);
        }

        void LockMouse(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            ignoreLookFrames = 2;
        }

        void Update()
        {
            simulatedFrames++;
            Vector2 move = Vector2.zero, look = Vector2.zero;
            bool jump = false, sprint = false, reset = false, escape = false, click = false, toggleView = false;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys != null)
            {
                move.x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
                move.y = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
                jump = keys.spaceKey.isPressed;
                sprint = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
                reset = keys.rKey.isPressed;
                escape = keys.escapeKey.isPressed;
                toggleView = keys.vKey.isPressed;
            }
            if (mouse != null) { look = mouse.delta.ReadValue(); click = mouse.leftButton.wasPressedThisFrame; }
#else
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 16;
            jump = Input.GetKey(KeyCode.Space); sprint = Input.GetKey(KeyCode.LeftShift);
            reset = Input.GetKey(KeyCode.R); escape = Input.GetKey(KeyCode.Escape); click = Input.GetMouseButtonDown(0);
            toggleView = Input.GetKey(KeyCode.V);
#endif
            jump = Pressed(jump, ref jumpHeld);
            reset = Pressed(reset, ref resetHeld);
            escape = Pressed(escape, ref escapeHeld);
            toggleView = Pressed(toggleView, ref viewHeld);
            if (escape) LockMouse(false);
            else if (click && (phone == null || !phone.PointerOverCallPanel)) LockMouse(true);
            if (toggleView && allowViewSwitch) { thirdPerson = !thirdPerson; pitch = thirdPerson ? 12 : 0; }
            if (!allowViewSwitch) thirdPerson = false;
            if (ignoreLookFrames > 0) { look = Vector2.zero; ignoreLookFrames--; }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0, look.x * mouseSensitivity, 0);
                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -85, 85);
            }
            else { move = Vector2.zero; jump = false; }
            if (body.isGrounded && vertical < 0) vertical = -2;
            if (body.isGrounded && jump) vertical = Mathf.Sqrt(jumpHeight * 2 * 22);
            vertical -= 22 * Time.deltaTime;
            Vector3 direction = transform.right * move.x + transform.forward * move.y;
            body.Move((Vector3.ClampMagnitude(direction, 1) * (sprint ? sprintSpeed : walkSpeed) + Vector3.up * vertical) * Time.deltaTime);
            if (reset || transform.position.y < -8) Respawn();
        }

        static bool Pressed(bool down, ref bool held)
        {
            bool pressed = down && !held;
            held = down;
            return pressed;
        }

        void LateUpdate()
        {
            if (avatar != null) avatar.gameObject.SetActive(thirdPerson);
            if (view == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 head = transform.TransformPoint(new Vector3(0, 1.55f, 0));
            Vector3 direction = transform.rotation * rotation * Vector3.back;
            float distance = thirdPerson ? cameraDistance : 0;
            if (thirdPerson)
            {
                foreach (var hit in Physics.SphereCastAll(head, .15f, direction, cameraDistance, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(transform)) distance = Mathf.Min(distance, Mathf.Max(.08f, hit.distance - .08f));
            }
            view.position = head + direction * distance;
            view.localRotation = rotation;
        }

        public void Respawn()
        {
            body.enabled = false;
            transform.SetPositionAndRotation(spawn, Quaternion.identity);
            body.enabled = true;
            pitch = vertical = 0;
            if (view != null) view.localRotation = Quaternion.identity;
        }

        void OnDisable() { LockMouse(false); }
        void OnApplicationFocus(bool focused) { if (focused) ignoreLookFrames = 3; }

        void OnGUI()
        {
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            GUI.Box(new Rect(18, 18, 460, 100), hudTitle);
            GUI.Label(new Rect(32, 45, 430, 22), "WASD / Arrows: Move    Mouse: Look    Shift: Run");
            GUI.Label(new Rect(32, 66, 430, 22), "Space: Jump    R: Respawn    Esc: Release mouse");
            GUI.Label(new Rect(32, 87, 430, 22), allowViewSwitch ? "V: First / Third person    View: " + (thirdPerson ? "THIRD PERSON" : "FIRST PERSON") : "FIRST PERSON    F: Draw / Stow flashlight    G: Light On / Off");
            GUI.Label(new Rect(635, 350, 20, 20), "+");
            if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Box(new Rect(495, 662, 290, 30), "Click the Game view to control the player");
            GUI.matrix = previous;
        }
    }
}
