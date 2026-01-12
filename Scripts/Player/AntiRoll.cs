using UnityEngine;

/// <summary>
/// Anti-roll bar compatible with the project's `Wheel` and `Drive` scripts.
/// 挂到与 Drive 同一 GameObject（或包含 Rigidbody 的父物体）上，配置左右轮对，FixedUpdate 中按悬挂行程差施加抗侧倾力。
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

            // 默认行程值（与原插件保持一致的初始值）
            float travelL = 1.0f;
            float travelR = 1.0f;

            // 计算行程（若悬挂距离为 0 则保留默认）
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

            // 限制行程到 [0,1]，防止异常值影响力的计算
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