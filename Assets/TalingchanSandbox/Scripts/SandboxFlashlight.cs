using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Talingchan.Sandbox
{
    [DefaultExecutionOrder(150)]
    public class SandboxFlashlight : MonoBehaviour
    {
        public enum EquipState { Holstered, Drawing, Equipped, Holstering }
        [Header("Left-hand flashlight")]
        public SandboxPlayer player;
        public Transform firstPersonRig;
        public Light beam;
        public Renderer lens;
        public float animationDuration = .45f;
        public float beamRange = 28;
        public float beamIntensity = 250;
        public bool powerRequested = true;
        public UnityEvent onDrawn = new UnityEvent();
        public UnityEvent onHolstered = new UnityEvent();
        public UnityEvent<bool> onLightChanged = new UnityEvent<bool>();
        [SerializeField] bool targetEquipped;
        [SerializeField, Range(0,1)] float progress;
        public EquipState State => targetEquipped ? (progress >= 1 ? EquipState.Equipped : EquipState.Drawing) : (progress <= 0 ? EquipState.Holstered : EquipState.Holstering);
        public bool IsOn => beam != null && beam.enabled;
        public float EquipProgress => progress;
        bool requestDraw, requestPower, priorOn;
#if !ENABLE_INPUT_SYSTEM
        bool drawHeld, powerHeld;
#endif
        float walkPhase;
        CharacterController body;
        MaterialPropertyBlock lensProperties;
#if ENABLE_INPUT_SYSTEM
        InputAction drawAction, powerAction;
#endif

        void Awake()
        {
            if(player == null) player = GetComponent<SandboxPlayer>();
            body = GetComponent<CharacterController>();
            lensProperties = new MaterialPropertyBlock();
#if ENABLE_INPUT_SYSTEM
            drawAction = new InputAction("Draw or stow left flashlight", InputActionType.Button, "<Keyboard>/f");
            powerAction = new InputAction("Toggle flashlight power", InputActionType.Button, "<Keyboard>/g");
            drawAction.performed += context => requestDraw = true;
            powerAction.performed += context => requestPower = true;
#endif
            ApplyVisibility();
        }
        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            drawAction?.Enable(); powerAction?.Enable();
#endif
        }
        void Update()
        {
            bool draw = false, power = false;
#if ENABLE_INPUT_SYSTEM
            draw = requestDraw;
            power = requestPower;
#else
            draw = Input.GetKey(KeyCode.F); power = Input.GetKey(KeyCode.G);
            draw = Pressed(draw, ref drawHeld);
            power = Pressed(power, ref powerHeld);
#endif
            requestDraw = requestPower = false;
            if(draw) ToggleEquipped();
            if(power) ToggleLight();
            float old = progress;
            progress = Mathf.MoveTowards(progress, targetEquipped ? 1 : 0, Time.deltaTime / Mathf.Max(.01f,animationDuration));
            if(progress == 1 && old < 1) onDrawn.Invoke();
            if(progress == 0 && old > 0) onHolstered.Invoke();
            ApplyVisibility();
        }
        public void ToggleEquipped() { SetEquipped(!targetEquipped); }
        public void SetEquipped(bool equipped) { targetEquipped = equipped; ApplyVisibility(); }
        public void ToggleLight() { if(targetEquipped) SetPower(!powerRequested); }
        public void SetPower(bool on) { powerRequested = on; ApplyVisibility(); }
        static bool Pressed(bool down, ref bool held) { bool edge=down&&!held;held=down;return edge; }
        void ApplyVisibility()
        {
            bool on = targetEquipped && progress > .72f && powerRequested;
            if(firstPersonRig != null) firstPersonRig.gameObject.SetActive(progress > .001f);
            if(beam != null) { beam.enabled=on;beam.range=beamRange;beam.intensity=beamIntensity; }
            if(lens != null)
            {
                if(lensProperties == null) lensProperties = new MaterialPropertyBlock();
                lensProperties.SetColor("_EmissionColor",on?new Color(1,.92f,.72f)*2:Color.black);
                lens.SetPropertyBlock(lensProperties);
            }
            if(on != priorOn) { priorOn=on;onLightChanged.Invoke(on); }
        }
        void LateUpdate()
        {
            if(firstPersonRig == null || progress <= 0) return;
            float t = Mathf.SmoothStep(0,1,progress);
            float speed = body == null ? 0 : new Vector2(body.velocity.x,body.velocity.z).magnitude;
            walkPhase += Time.deltaTime*speed*2;
            Vector3 bob = new Vector3(Mathf.Sin(walkPhase)*.005f,Mathf.Sin(walkPhase*2)*.004f,0)*Mathf.Clamp01(speed);
            firstPersonRig.localPosition = Vector3.Lerp(new Vector3(-.35f,-.75f,.35f),new Vector3(-.27f,-.24f,.52f),t)+bob*t;
            firstPersonRig.localRotation = Quaternion.Slerp(Quaternion.Euler(35,-20,-25),Quaternion.Euler(-4,3,0),t);
            if(beam != null && player != null && player.view != null)
                beam.transform.rotation = Quaternion.LookRotation(player.view.position+player.view.forward*15-beam.transform.position,player.view.up);
        }
        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            drawAction?.Disable();powerAction?.Disable();
#endif
            requestDraw=requestPower=false;
            if(beam != null) beam.enabled=false;
            if(firstPersonRig != null) firstPersonRig.gameObject.SetActive(false);
        }
        void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            drawAction?.Dispose();powerAction?.Dispose();
#endif
        }
        void OnGUI()
        {
            Matrix4x4 prior=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.Box(new Rect(18,630,400,68),"");
            var style=new GUIStyle(GUI.skin.label){fontSize=16};
            GUI.Label(new Rect(32,638,370,25),"LEFT FLASHLIGHT  /  "+(State==EquipState.Holstered?"STOWED":State==EquipState.Drawing?"DRAWING...":State==EquipState.Holstering?"STOWING...":IsOn?"ON":"OFF"),style);
            GUI.Label(new Rect(32,664,370,24),"F: Draw / Stow     G: Switch On / Off",style);
            GUI.matrix=prior;
        }
    }
}
