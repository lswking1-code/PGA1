using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Drive : MonoBehaviour
{
    [Header("Wheels")]
    public WheelCollider[] wheelColliders;   // 对应顺序：前左、前右、后左、后右（或项目自定义）
    public GameObject[] wheels;              // 视觉模型（可选，同步到 wheelColliders）

    [Header("Performance")]
    [Range(0, 2000)] public float engineTorque = 400f;   // 基础扭矩系数
    [Range(0, 2000)] public float brakeTorque = 500f;    // 最大制动力（会平均分配到制动轮）
    public float finalDriveRatio = 3.2f;
    public float maximumSpeed = 160f;                    // km/h
    public float highSpeedSteerAngle = 100f;             // 在此速度处转向角降到最小

    [Header("Engine RPM")]
    public float minimumEngineRPM = 650f;
    public float maximumEngineRPM = 7000f;
    [SerializeField] private float _currentEngineRPM = 0f;
    public float currentEngineRPM { get { return _currentEngineRPM; } private set { _currentEngineRPM = value; } }

    [Header("Physics")]
    public Transform COM;                 // 质心（可在 Inspector 设置）
    [Range(-2f, 0f)] public float centerOfMassY = -0.5f;
    public float downforce = 100f;
    [Range(0f, 10f)] public float antiRolloverForce = 3f;
    [Range(0f, 90f)] public float maxTiltAngle = 45f;

    [Header("Steering")]
    [Range(0f, 60f)]
    public float SteeringAngle = 30f;

    // 运行时状态（对外只读）
    [SerializeField] private float _speed = 0f; // km/h
    public float speed { get { return _speed; } private set { _speed = value; } }

    [SerializeField] private int _direction = 1; // 1 = forward, -1 = reverse
    public int direction { get { return _direction; } private set { _direction = value; } }

    // 内部
    private Rigidbody rb;
    private float timerForReverse = 0f;
    private bool appliedBrake = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (COM != null && rb != null)
            rb.centerOfMass = COM.localPosition;
        else if (rb != null)
            rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);
    }

    /// <summary>
    /// 主驱动接口（保持现有签名以兼容 DriveController）
    /// acceleration: 0..1, brake: 0..1, steer: Vector2 (x 用作左右)
    /// </summary>
    public void Driving(float acceleration, float brake, Vector2 steer)
    {
        if (rb == null || wheelColliders == null || wheelColliders.Length == 0) return;

        // 更新质心（支持运行时调整）
        if (COM != null) rb.centerOfMass = COM.localPosition;

        // 计算速度（km/h）
        speed = rb.linearVelocity.magnitude * 3.6f;

        // 倒车判定（低速长按刹车触发）
        ReverseLogic(brake);

        // 发动机转速计算（以驱动轮 rpm 平均值为依据）
        EngineRPMCalculation();

        // 转向
        ApplySteering(steer.x);

        // 牵引与制动
        ApplyTractionAndBrakes(acceleration, brake);

        // 下压力与防侧翻
        AddDownforce();
        ApplyAntiRollover();

        // 视觉轮同步（可选）
        UpdateVisualWheels();
    }

    private void EngineRPMCalculation()
    {
        float sumRPM = 0f;
        int driveCount = 0;
        foreach (var wc in wheelColliders)
        {
            if (wc == null) continue;
            // 只有当轮被驱动（通过 motorTorque 有效）才计入：这里以 engine 固定驱动所有设置驱动轮为例
            // 简化：以所有非-null 的轮子做平均，或按需只计 drive 轮
            sumRPM += Mathf.Abs(wc.rpm);
            driveCount++;
        }

        if (driveCount == 0)
        {
            currentEngineRPM = minimumEngineRPM;
            return;
        }

        float avgRPM = sumRPM / driveCount; // wheel rpm
        // 将 wheel rpm 映射到发动机 rpm（简单线性映射）
        // 这里把 wheel rpm / 60 -> rps，再依照最大速度做归一化（防止过大波动）
        float normalized = Mathf.Clamp01((avgRPM / 60f) / (maximumSpeed / 3.6f + 0.0001f));
        currentEngineRPM = Mathf.Lerp(minimumEngineRPM, maximumEngineRPM, normalized);
        currentEngineRPM = Mathf.Clamp(currentEngineRPM, minimumEngineRPM, maximumEngineRPM);
    }

    private void ApplySteering(float steerInput)
    {
        float speedFactor = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(speed / highSpeedSteerAngle));
        float targetAngle = steerInput * (wheelColliders.Length > 0 ? wheelColliders[0].steerAngle : 30f); // fallback
        foreach (var wc in wheelColliders)
        {
            if (wc == null) continue;
            // 只对前两个轮（通常为前轴）生效；若项目轮序不同请在 Inspector 设置
            int idx = System.Array.IndexOf(wheelColliders, wc);
            if (idx >= 0 && idx < 2) // 0、1 为前轮
                wc.steerAngle = steerInput * (idx >= 0 ? wc.suspensionDistance * 0f : 0f); // placeholder：不覆写默认 steerAngle
        }

        // 更明确的做法：给前两轮设置 steerAngle = SteeringAngle * steerInput * speedFactor
        float applied = SteeringAngle * steerInput * speedFactor;
        for (int i = 0; i < wheelColliders.Length && i < 2; i++)
        {
            if (wheelColliders[i] == null) continue;
            wheelColliders[i].steerAngle = applied;
        }
    }

    // 牵引与刹车合并处理，平均分配扭矩/刹车到对应轮
    private void ApplyTractionAndBrakes(float throttleInput, float brakeInput)
    {
        // 统计驱动轮与制动轮数
        int driveCount = 0, brakeCount = 0;
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] == null) continue;
            // 简化假设：所有轮都用于驱动与制动，若需要可改为按索引或额外数组标记
            driveCount++;
            brakeCount++;
        }
        driveCount = Mathf.Max(1, driveCount);
        brakeCount = Mathf.Max(1, brakeCount);

        // 计算驱动力输入（前进时用油门，倒挡时用刹车作为反向动力）
        float driveInput = (direction == 1) ? Mathf.Clamp01(throttleInput) : -Mathf.Clamp01(brakeInput);
        float totalTorque = engineTorque * finalDriveRatio * driveInput;

        // 分配到每个驱动轮
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            var wc = wheelColliders[i];
            if (wc == null) continue;

            // 驱动力
            float perWheelTorque = totalTorque / driveCount;

            // 如果任一轮的计算转速 >= 最大速度，则不再施加驱动力
            float wheelSpeedKmh = (Mathf.Abs(wc.rpm) / 60f) * (2f * Mathf.PI * Mathf.Max(wc.radius, 0.1f)) * 3.6f;
            if (speed >= maximumSpeed || wheelSpeedKmh >= maximumSpeed)
                wc.motorTorque = 0f;
            else
                wc.motorTorque = perWheelTorque;

            // 制动力：前进时使用 brakeInput，倒车时用 throttleInput（与 SCC_Drivetrain 行为一致）
            float inputForBrake = (direction == 1) ? brakeInput : throttleInput;
            float perWheelBrake = (brakeTorque * Mathf.Clamp01(inputForBrake)) / brakeCount;
            wc.brakeTorque = perWheelBrake;

            if (perWheelBrake >= 5f) appliedBrake = true;
        }
    }

    private void ReverseLogic(float brakeInput)
    {
        // 使用车体前向速度判断，避免线性速度的正负误判
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward); // m/s
        float forwardKmh = forwardSpeed * 3.6f;

        if (Mathf.Abs(forwardKmh) <= 5f && brakeInput >= 0.75f)
            timerForReverse += Time.fixedDeltaTime;
        else if (Mathf.Abs(forwardKmh) <= 5f && brakeInput <= 0.25f)
            timerForReverse = 0f;

        _direction = (timerForReverse >= 0.4f) ? -1 : 1;
    }

    private void AddDownforce()
    {
        if (rb == null) return;
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
    }

    private void ApplyAntiRollover()
    {
        if (rb == null) return;
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        if (tiltAngle > maxTiltAngle)
        {
            Vector3 corrective = Vector3.Cross(transform.up, Vector3.up) * antiRolloverForce;
            rb.AddTorque(corrective, ForceMode.Acceleration);
        }
    }

    private void UpdateVisualWheels()
    {
        if (wheels == null || wheelColliders == null) return;
        int len = Mathf.Min(wheels.Length, wheelColliders.Length);
        for (int i = 0; i < len; i++)
        {
            if (wheels[i] == null || wheelColliders[i] == null) continue;
            Vector3 pos;
            Quaternion rot;
            wheelColliders[i].GetWorldPose(out pos, out rot);
            wheels[i].transform.position = pos;
            wheels[i].transform.rotation = rot;
        }
    }

    // Debug / utility
    public float GetForwardSpeed()
    {
        return Vector3.Dot(rb.linearVelocity, transform.forward);
    }

    public float GetCurrentSpeed()
    {
        return Vector3.Dot(rb.linearVelocity, transform.forward);
    }
}
