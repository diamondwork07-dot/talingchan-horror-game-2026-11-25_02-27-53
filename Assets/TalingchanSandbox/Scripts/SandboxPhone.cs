using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Talingchan.Sandbox
{
    // Gameplay can subscribe to onAnswered: it fires only after the phone reaches the ear.
    [DefaultExecutionOrder(100)]
    public class SandboxPhone : MonoBehaviour
    {
        public enum CallState { Idle, Ringing, PickingUp, RaisingToEar, InCall, PuttingAway }
        [Header("Demo incoming call")]
        public bool autoDemoCall = true;
        public float firstCallDelay = 6;
        public string demoCaller = "UNKNOWN CALLER";
        public float ringTimeout = 20;
        [Header("Animation timing")]
        public float pickupDuration = .65f;
        public float raiseDuration = .8f;
        public float putAwayDuration = .7f;
        [Header("Rig")]
        public SandboxPlayer player;
        public Transform shoulder, forearm, hand, handset, firstPersonRig;
        public AudioSource ringtone;
        [Header("Connect dialogue / quest code here")]
        public UnityEvent onRinging = new UnityEvent();
        public UnityEvent onAnswered = new UnityEvent();
        public UnityEvent onDeclined = new UnityEvent();
        public UnityEvent onMissed = new UnityEvent();
        public UnityEvent onEnded = new UnityEvent();
        [SerializeField] CallState state;
        public CallState State => state;
        public string Caller { get; private set; }
        public float CallSeconds { get; private set; }
        public float RingSecondsRemaining => Mathf.Max(0, ringTimeout - timer);
        public bool IsAnimating => state == CallState.PickingUp || state == CallState.RaisingToEar || state == CallState.PuttingAway;
        public bool PointerOverCallPanel
        {
            get
            {
                if (state == CallState.Idle || Cursor.lockState == CursorLockMode.Locked) return false;
                Vector2 point;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current == null) return false;
                point = Mouse.current.position.ReadValue();
#else
                point = Input.mousePosition;
#endif
                point = new Vector2(point.x * 1280 / Screen.width, (Screen.height - point.y) * 720 / Screen.height);
                return new Rect(918, 394, 342, 304).Contains(point);
            }
        }
        float timer, demoTimer, noticeTimer;
        bool demoSent, answerHeld, declineHeld, testHeld;
        bool pendingAnswer, pendingDecline, pendingTest;
#if ENABLE_INPUT_SYSTEM
        InputAction answerAction, declineAction, testAction;
#endif
        string notice = "";
        Vector3 loweringFrom;
        AudioClip generatedRing;
        readonly Vector3 pocket = new Vector3(.35f, .88f, .08f);
        readonly Vector3 chest = new Vector3(.34f, 1.17f, .28f);
        readonly Vector3 ear = new Vector3(.35f, 1.49f, .04f);

        void Awake()
        {
            if (player == null) player = GetComponent<SandboxPlayer>();
#if ENABLE_INPUT_SYSTEM
            answerAction = new InputAction("Answer phone", InputActionType.Button, "<Keyboard>/e");
            declineAction = new InputAction("Decline or end call", InputActionType.Button, "<Keyboard>/q");
            testAction = new InputAction("Test incoming call", InputActionType.Button, "<Keyboard>/t");
            answerAction.performed += context => pendingAnswer = true;
            declineAction.performed += context => pendingDecline = true;
            testAction.performed += context => pendingTest = true;
#endif
            if (ringtone != null)
            {
                generatedRing = MakeRingtone();
                ringtone.clip = generatedRing;
                ringtone.loop = true;
                ringtone.spatialBlend = 0;
                ringtone.volume = .2f;
                ringtone.playOnAwake = false;
            }
        }

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            answerAction?.Enable(); declineAction?.Enable(); testAction?.Enable();
#endif
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (noticeTimer > 0) noticeTimer -= dt;
            if (!demoSent && autoDemoCall)
            {
                demoTimer += dt;
                if (demoTimer >= firstCallDelay && state == CallState.Idle)
                { demoSent = true; BeginIncomingCall(demoCaller); }
            }
            bool answer = false, decline = false, test = false;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            if (keys != null) { answer = keys.eKey.isPressed; decline = keys.qKey.isPressed; test = keys.tKey.isPressed; }
#else
            answer = Input.GetKey(KeyCode.E); decline = Input.GetKey(KeyCode.Q); test = Input.GetKey(KeyCode.T);
#endif
            answer = Pressed(answer, ref answerHeld);
            decline = Pressed(decline, ref declineHeld);
            test = Pressed(test, ref testHeld);
            answer |= pendingAnswer; decline |= pendingDecline; test |= pendingTest;
            pendingAnswer = pendingDecline = pendingTest = false;
            if (test && state == CallState.Idle) { demoSent = true; BeginIncomingCall(demoCaller); }
            if (answer && state == CallState.Ringing) AnswerCall();
            if (decline)
            {
                if (state == CallState.Ringing) DeclineCall();
                else EndCall();
            }
            timer += dt;
            switch (state)
            {
                case CallState.Ringing:
                    if (timer >= Mathf.Max(.1f, ringTimeout))
                    { StopRing(); SetState(CallState.Idle); Notify("MISSED CALL"); onMissed.Invoke(); }
                    break;
                case CallState.PickingUp:
                    if (timer >= Mathf.Max(.01f, pickupDuration)) SetState(CallState.RaisingToEar);
                    break;
                case CallState.RaisingToEar:
                    if (timer >= Mathf.Max(.01f, raiseDuration))
                    {
                        SetState(CallState.InCall);
                        CallSeconds = 0;
                        // Pose is already at the ear before consumers start their dialogue.
                        ApplyPose(ear, 1);
                        onAnswered.Invoke();
                    }
                    break;
                case CallState.InCall: CallSeconds += dt; break;
                case CallState.PuttingAway:
                    if (timer >= Mathf.Max(.01f, putAwayDuration)) SetState(CallState.Idle);
                    break;
            }
        }

        public bool BeginIncomingCall(string caller)
        {
            if (state != CallState.Idle) return false;
            Caller = string.IsNullOrWhiteSpace(caller) ? "UNKNOWN CALLER" : caller;
            CallSeconds = 0;
            noticeTimer = 0;
            SetState(CallState.Ringing);
            if (ringtone != null) ringtone.Play();
            onRinging.Invoke();
            return true;
        }

        public void AnswerCall()
        {
            if (state != CallState.Ringing) return;
            StopRing();
            SetState(CallState.PickingUp);
        }

        public void DeclineCall()
        {
            if (state != CallState.Ringing) return;
            StopRing();
            SetState(CallState.Idle);
            Notify("CALL DECLINED");
            onDeclined.Invoke();
        }

        public void EndCall()
        {
            if (state != CallState.PickingUp && state != CallState.RaisingToEar && state != CallState.InCall) return;
            loweringFrom = CurrentHandPosition();
            StopRing();
            SetState(CallState.PuttingAway);
            Notify("CALL ENDED");
            onEnded.Invoke();
        }

        void SetState(CallState next) { state = next; timer = 0; }
        void Notify(string message) { notice = message; noticeTimer = 4; }
        void StopRing() { if (ringtone != null) ringtone.Stop(); }
        static bool Pressed(bool down, ref bool held) { bool edge = down && !held; held = down; return edge; }
        static float Ease(float t) { return Mathf.SmoothStep(0, 1, Mathf.Clamp01(t)); }

        Vector3 CurrentHandPosition()
        {
            if (state == CallState.PickingUp)
            {
                float t = Mathf.Clamp01(timer / Mathf.Max(.01f, pickupDuration));
                if (t < .35f) return Vector3.Lerp(new Vector3(.35f, .80f, 0), pocket, Ease(t / .35f));
                return Vector3.Lerp(pocket, chest, Ease((t - .35f) / .65f));
            }
            if (state == CallState.RaisingToEar) return Vector3.Lerp(chest, ear, Ease(timer / Mathf.Max(.01f, raiseDuration)));
            if (state == CallState.PuttingAway) return Vector3.Lerp(loweringFrom, pocket, Ease(timer / Mathf.Max(.01f, putAwayDuration)));
            return ear;
        }

        void LateUpdate()
        {
            bool visible = IsAnimating || state == CallState.InCall;
            if (handset != null) handset.gameObject.SetActive(visible);
            if (firstPersonRig != null) firstPersonRig.gameObject.SetActive(visible && player != null && !player.thirdPerson);
            if (visible) ApplyPose(CurrentHandPosition(), state == CallState.PickingUp ? Ease(timer / Mathf.Max(.01f, pickupDuration)) : 1);
        }

        void ApplyPose(Vector3 localHand, float pickupBlend)
        {
            if (player == null || shoulder == null || forearm == null || hand == null) return;
            Transform actor = player.transform;
            Vector3 target = actor.TransformPoint(localHand);
            Vector3 start = shoulder.position;
            const float upper = .28f, lower = .26f;
            Vector3 delta = target - start;
            float distance = Mathf.Clamp(delta.magnitude, .03f, upper + lower - .001f);
            Vector3 axis = delta.normalized;
            Vector3 pole = actor.TransformDirection(new Vector3(.35f, 0, 1));
            pole = Vector3.ProjectOnPlane(pole, axis).normalized;
            float a = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            Vector3 elbow = start + axis * a + pole * Mathf.Sqrt(Mathf.Max(0, upper * upper - a * a));
            shoulder.rotation = Quaternion.FromToRotation(Vector3.down, elbow - start);
            forearm.rotation = Quaternion.FromToRotation(Vector3.down, target - elbow);
            if (handset != null)
            {
                Vector3 phoneHand = localHand;
                if (state == CallState.PickingUp && timer / Mathf.Max(.01f, pickupDuration) < .35f) phoneHand = pocket;
                handset.position = actor.TransformPoint(phoneHand + Vector3.Lerp(new Vector3(.015f,.08f,0),new Vector3(-.085f,.10f,0),pickupBlend));
                handset.rotation = actor.rotation * Quaternion.Euler(0, 0, Mathf.Lerp(-25, 0, pickupBlend));
            }
            if (firstPersonRig != null)
            {
                float height = Mathf.InverseLerp(pocket.y, ear.y, localHand.y);
                firstPersonRig.localPosition = Vector3.Lerp(new Vector3(.29f,-.57f,.58f),new Vector3(.40f,-.11f,.16f),Ease(height));
                firstPersonRig.localRotation = Quaternion.Euler(0,-20,Mathf.Lerp(-28,5,height));
            }
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            answerAction?.Disable(); declineAction?.Disable(); testAction?.Disable();
#endif
            StopRing();
            if (handset != null) handset.gameObject.SetActive(false);
            if (firstPersonRig != null) firstPersonRig.gameObject.SetActive(false);
            state = CallState.Idle;
        }
        void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            answerAction?.Dispose(); declineAction?.Dispose(); testAction?.Dispose();
#endif
            if (generatedRing != null) Destroy(generatedRing);
        }

        static AudioClip MakeRingtone()
        {
            const int rate = 22050;
            float[] samples = new float[rate * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float beat = t < .19f ? t : (t >= .30f && t < .49f ? t - .30f : -1);
                if (beat >= 0)
                {
                    float envelope = Mathf.Clamp01(beat / .012f) * Mathf.Clamp01((.19f - beat) / .025f);
                    samples[i] = envelope * (Mathf.Sin(t * 2 * Mathf.PI * 660) + .5f * Mathf.Sin(t * 2 * Mathf.PI * 880)) * .35f;
                }
            }
            var clip = AudioClip.Create("Sandbox phone ring", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnGUI()
        {
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            var title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var line = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            if (state == CallState.Idle)
            {
                GUI.Box(new Rect(918, 654, 342, 44), "");
                GUI.Label(new Rect(924, 660, 330, 30), noticeTimer > 0 ? notice : "T: Trigger a test incoming call", line);
            }
            else
            {
                GUI.Box(new Rect(918, 394, 342, 304), "");
                GUI.Label(new Rect(936, 409, 306, 35), state == CallState.Ringing ? "INCOMING CALL" : "PHONE", title);
                GUI.Label(new Rect(936, 453, 306, 35), Caller ?? demoCaller, title);
                string status = "";
                switch (state)
                {
                    case CallState.Ringing: status = "Ringing... " + Mathf.CeilToInt(RingSecondsRemaining) + "s"; break;
                    case CallState.PickingUp: status = "Taking phone out..."; break;
                    case CallState.RaisingToEar: status = "Raising phone to ear..."; break;
                    case CallState.InCall: status = "CONNECTED  " + System.TimeSpan.FromSeconds(CallSeconds).ToString(@"mm\:ss"); break;
                    case CallState.PuttingAway: status = "Putting phone away..."; break;
                }
                GUI.Label(new Rect(936, 500, 306, 30), status, line);
                if (state == CallState.Ringing)
                {
                    GUI.backgroundColor = new Color(.22f,.75f,.44f);
                    if (GUI.Button(new Rect(936, 547, 145, 49), "E  ANSWER", button)) AnswerCall();
                    GUI.backgroundColor = new Color(.88f,.30f,.30f);
                    if (GUI.Button(new Rect(1093, 547, 149, 49), "Q  DECLINE", button)) DeclineCall();
                    GUI.backgroundColor = Color.white;
                    GUI.Label(new Rect(936, 612, 306, 56), "Ignore to miss the call.\nEsc releases the mouse for buttons.", line);
                }
                else if (state != CallState.PuttingAway)
                {
                    GUI.backgroundColor = new Color(.88f,.30f,.30f);
                    if (GUI.Button(new Rect(936, 547, 306, 49), "Q  END CALL", button)) EndCall();
                    GUI.backgroundColor = Color.white;
                    GUI.Label(new Rect(936, 610, 306, 56), state == CallState.InCall ? "Call in progress.\nPress Q to hang up." : "Connecting after the animation...", line);
                }
            }
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
            GUI.matrix = previous;
        }
    }
}
