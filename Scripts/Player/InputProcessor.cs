using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("PGA1/Input Processor")]
public class InputProcessor : MonoBehaviour
{
    [System.Serializable]
    public class DriveInputs
    {
        public float throttleInput; // 0..1
        public float steerInput;    // -1..1
        public float brakeInput;    // 0..1
    }

    public DriveInputs inputs = new DriveInputs();

    [Header("Source")]
    public bool receiveFromLegacy = true;           // 使用旧输入轴（Vertical/Horizontal）
    public bool receiveFromPlayerInput = false;     // 使用 Input System（优先）
    public PlayerInput playerInput;                 // 可选，若使用 Input System 则赋值

    [Header("Input Action Names (PlayerInput)")]
    public string throttleAction = "Accelerate";    // float
    public string brakeAction = "Brake";            // float
    public string steerAction = "Steer";            // Vector2 或 float（取 x）

    [Header("Smoothing")]
    public bool smoothInputs = true;
    [Tooltip("变化速率（单位：/秒），值越大收敛越快")]
    public float smoothingFactor = 5f;

    private void Update()
    {
        if (inputs == null) inputs = new DriveInputs();

        float targetThrottle = 0f;
        float targetBrake = 0f;
        float targetSteer = 0f;

        // 优先 PlayerInput（如果启用并赋值）
        if (receiveFromPlayerInput && playerInput != null && playerInput.actions != null)
        {
            var a_throttle = playerInput.actions.FindAction(throttleAction);
            var a_brake = playerInput.actions.FindAction(brakeAction);
            var a_steer = playerInput.actions.FindAction(steerAction);

            if (a_throttle != null) targetThrottle = a_throttle.ReadValue<float>();
            if (a_brake != null) targetBrake = a_brake.ReadValue<float>();

            if (a_steer != null)
            {
                var controlType = a_steer.activeControl?.valueType;
                if (controlType == typeof(Vector2))
                    targetSteer = a_steer.ReadValue<Vector2>().x;
                else
                    targetSteer = a_steer.ReadValue<float>();
            }
        }
        else if (receiveFromLegacy)
        {
            float v = Input.GetAxis("Vertical");
            targetThrottle = Mathf.Clamp01(v);
            targetBrake = Mathf.Clamp01(-v);
            targetSteer = Input.GetAxis("Horizontal");
        }

        if (smoothInputs)
        {
            inputs.throttleInput = Mathf.MoveTowards(inputs.throttleInput, targetThrottle, Time.deltaTime * smoothingFactor);
            inputs.brakeInput = Mathf.MoveTowards(inputs.brakeInput, targetBrake, Time.deltaTime * smoothingFactor);
            inputs.steerInput = Mathf.MoveTowards(inputs.steerInput, targetSteer, Time.deltaTime * smoothingFactor);
        }
        else
        {
            inputs.throttleInput = targetThrottle;
            inputs.brakeInput = targetBrake;
            inputs.steerInput = targetSteer;
        }
    }

    /// <summary>
    /// 覆写输入（外部可调用）。若 smoothInputs 为 true，将按 smoothingFactor 平滑过渡。
    /// </summary>
    public void OverrideInputs(DriveInputs newInputs)
    {
        if (newInputs == null) return;

        if (!smoothInputs)
        {
            inputs.throttleInput = newInputs.throttleInput;
            inputs.brakeInput = newInputs.brakeInput;
            inputs.steerInput = newInputs.steerInput;
        }
        else
        {
            inputs.throttleInput = Mathf.MoveTowards(inputs.throttleInput, newInputs.throttleInput, Time.deltaTime * smoothingFactor);
            inputs.brakeInput = Mathf.MoveTowards(inputs.brakeInput, newInputs.brakeInput, Time.deltaTime * smoothingFactor);
            inputs.steerInput = Mathf.MoveTowards(inputs.steerInput, newInputs.steerInput, Time.deltaTime * smoothingFactor);
        }
    }
}