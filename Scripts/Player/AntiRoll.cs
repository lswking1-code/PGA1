using UnityEngine;

/// <summary>
/// Anti-roll bar compatible with the project's `Wheel` and `Drive` scripts.
/// Should be placed on the same GameObject as Drive (or the parent GameObject with Rigidbody), works in pairs, applies anti-roll force based on suspension travel in FixedUpdate.
/// </summary>
[AddComponentMenu("PGA2/Player/AntiRoll")]
[RequireComponent(typeof(Rigidbody))]
public class AntiRoll : MonoBehaviour
{
    [System.Serializable]
    public class WheelPair
    {
        public Wheel leftWheel;
        public Wheel rightWheel;
        public float force = 1000f;
    }

    [Header("Pairs (left / right)")]
    public WheelPair[] pairs;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (rb == null || pairs == null) return;

        for (int i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i];
            if (pair == null) continue;
            if (pair.leftWheel == null || pair.rightWheel == null) continue;
            if (pair.leftWheel.wheelCollider == null || pair.rightWheel.wheelCollider == null) continue;

            WheelHit leftHit, rightHit;
            bool groundedL = pair.leftWheel.wheelCollider.GetGroundHit(out leftHit);
            bool groundedR = pair.rightWheel.wheelCollider.GetGroundHit(out rightHit);

            // Default travel value, consistent with original default initial value
            float travelL = 1.0f;
            float travelR = 1.0f;

            // Calculate travel, if not grounded or 0, use default
            float suspL = pair.leftWheel.wheelCollider.suspensionDistance;
            if (groundedL && suspL > 0f)
            {
                travelL = (-pair.leftWheel.transform.InverseTransformPoint(leftHit.point).y - pair.leftWheel.wheelCollider.radius) / suspL;
            }

            float suspR = pair.rightWheel.wheelCollider.suspensionDistance;
            if (groundedR && suspR > 0f)
            {
                travelR = (-pair.rightWheel.transform.InverseTransformPoint(rightHit.point).y - pair.rightWheel.wheelCollider.radius) / suspR;
            }

            // Clamp travel to [0,1] to prevent abnormal values from affecting force calculation
            travelL = Mathf.Clamp01(travelL);
            travelR = Mathf.Clamp01(travelR);

            float antiRoll = (travelL - travelR) * pair.force;

            if (groundedL)
            {
                rb.AddForceAtPosition(pair.leftWheel.transform.up * -antiRoll, pair.leftWheel.transform.position, ForceMode.Force);
            }

            if (groundedR)
            {
                rb.AddForceAtPosition(pair.rightWheel.transform.up * antiRoll, pair.rightWheel.transform.position, ForceMode.Force);
            }
        }
    }
}
