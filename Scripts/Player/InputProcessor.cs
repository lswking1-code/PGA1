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
    public bool receiveFromLegacy = true;           // Use legacy input (Vertical/Horizontal)
    public bool receiveFromPlayerInput = false;     // Use Input System (priority)
    public PlayerInput playerInput;                 // Optional, assign when using Input System

    [Header("Input Action Names (PlayerInput)")]
    public string throttleAction = "Accelerate";    // float
    public string brakeAction = "Brake";            // float
    public string steerAction = "Steer";            // Vector2 or float, takes x

    [Header("Smoothing")]
    public bool smoothInputs = true;
    [Tooltip("Change rate (units/second), higher values are smoother")]
    public float smoothingFactor = 5f;

    private void Update()
    {
        if (inputs == null) inputs = new DriveInputs();

        float targetThrottle = 0f;
        float targetBrake = 0f;
        float targetSteer = 0f;

        // Read from PlayerInput, if not set, use legacy
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
    /// Override input (can be called externally), if smoothInputs is true, uses smoothingFactor for smoothing.
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
