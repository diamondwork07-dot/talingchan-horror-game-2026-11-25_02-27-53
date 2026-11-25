using UnityEngine;

namespace Talingchan.Sandbox
{
    public class SandboxAvatar : MonoBehaviour
    {
        public Transform leftArm, rightArm, leftLeg, rightLeg;
        public Transform rightForearm;
        CharacterController body;
        float phase;
        void Start() { body = GetComponentInParent<CharacterController>(); }
        void Update()
        {
            if (body == null) return;
            float speed = new Vector2(body.velocity.x, body.velocity.z).magnitude;
            phase += Time.deltaTime * speed * 2.8f;
            float swing = body.isGrounded ? Mathf.Sin(phase) * Mathf.Min(speed * 7, 32) : 12;
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(swing, 0, -5);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-swing, 0, 5);
            if (rightForearm != null) rightForearm.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(swing, 0, 0);
        }
    }
}
