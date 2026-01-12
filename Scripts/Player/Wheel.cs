using UnityEngine;

/// <summary>
/// 简化版的车轮组件：绑定 WheelCollider 与视觉模型，提供轮子状态（rpm、isGrounded、换算速度）
/// 与现有 Drive.cs 协同：Drive 仍使用 wheelColliders/wheels 数组；将视觉模型的 Transform 指向此组件的 wheelModel 可自动对齐。
/// </summary>
[RequireComponent(typeof(WheelCollider))]
public class Wheel : MonoBehaviour
{
    [Header("References")]
    public WheelCollider wheelCollider;
    public Transform wheelModel; // 可为空，但若不为空则会自动同步位置与旋转

    // 运行时状态（外部可读）
    internal bool isGrounded = false;
    internal float rpm = 0f;
    internal float wheelRPMToSpeed = 0f; // 用于驱动/engine 计算的速度换算

    private float wheelRotation = 0f;
    private Rigidbody cachedRb;

    private void Awake()
    {
        if (wheelCollider == null) wheelCollider = GetComponent<WheelCollider>();
        cachedRb = GetComponentInParent<Rigidbody>();

        if (wheelCollider == null)
        {
            Debug.LogError($"Wheel on '{name}' requires a WheelCollider. Disabling.");
            enabled = false;
            return;
        }
    }

    private void FixedUpdate()
    {
        WheelHit hit;
        // GetGroundHit 在未接地时会返回 false，hit 内容不可依赖
        isGrounded = wheelCollider.GetGroundHit(out hit);

        rpm = wheelCollider.rpm;

        float forwardSlip = isGrounded ? hit.forwardSlip : 0f;
        float lossyY = cachedRb ? cachedRb.transform.lossyScale.y : 1f;

        // 保持与原插件相似的换算逻辑（可按需调整系数）
        wheelRPMToSpeed = (((rpm * Mathf.Max(wheelCollider.radius, 0.001f)) / 2.8f) * Mathf.Lerp(1f, .75f, forwardSlip)) * lossyY;
    }

    private void Update()
    {
        // 在 Update 中同步视觉模型以获得更平滑的表现
        UpdateVisual();
    }

    /// <summary>
    /// 将视觉模型对齐到 WheelCollider 的世界位姿并应用自转与转向角
    /// </summary>
    public void UpdateVisual()
    {
        if (wheelModel == null || wheelCollider == null) return;

        // 使用 WheelCollider 的世界位姿（位置 + 旋转）
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelModel.position = pos;

        // 叠加自转（基于 rpm）与转向角（steerAngle）
        wheelRotation += wheelCollider.rpm * 6f * Time.deltaTime;
        wheelModel.rotation = rot * Quaternion.Euler(wheelRotation, wheelCollider.steerAngle, 0f);
    }

    /// <summary>
    /// 返回此轮当前的线速度（km/h），Drive 中可以用来判断轮速限制
    /// </summary>
    public float GetWheelSpeedKmh()
    {
        if (wheelCollider == null) return 0f;
        return (Mathf.Abs(rpm) / 60f) * (2f * Mathf.PI * Mathf.Max(wheelCollider.radius, 0.001f)) * 3.6f;
    }
}