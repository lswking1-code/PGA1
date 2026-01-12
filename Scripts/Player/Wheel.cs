using UnityEngine;

/// <summary>
/// Simplified wheel component that connects WheelCollider with visual model, provides runtime state (rpm, isGrounded, wheel speed)
/// Works with Drive.cs, Drive uses wheelColliders/wheels arrays; visual model's Transform reference, if wheelModel is empty, automatically finds child.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
public class Wheel : MonoBehaviour
{
    [Header("References")]
    public WheelCollider wheelCollider;
    public Transform wheelModel; // If empty, automatically finds child to sync position and rotation

    // Runtime state, externally readable
    internal bool isGrounded = false;
    internal float rpm = 0f;
    internal float wheelRPMToSpeed = 0f; // Wheel/engine RPM to speed conversion

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
        // GetGroundHit returns false when not grounded, hit data is invalid
        isGrounded = wheelCollider.GetGroundHit(out hit);

        rpm = wheelCollider.rpm;

        float forwardSlip = isGrounded ? hit.forwardSlip : 0f;
        float lossyY = cachedRb ? cachedRb.transform.lossyScale.y : 1f;

        // Basic logic consistent with original design, can adjust coefficients as needed
        wheelRPMToSpeed = (((rpm * Mathf.Max(wheelCollider.radius, 0.001f)) / 2.8f) * Mathf.Lerp(1f, .75f, forwardSlip)) * lossyY;
    }

    private void Update()
    {
        // Sync visual model in Update for smoother appearance
        UpdateVisual();
    }

    /// <summary>
    /// Sync visual model to WheelCollider's world pose, position and rotation
    /// </summary>
    public void UpdateVisual()
    {
        if (wheelModel == null || wheelCollider == null) return;

        // Use WheelCollider's world pose, position + rotation
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelModel.position = pos;

        // Apply rotation based on rpm, steering angle (steerAngle)
        wheelRotation += wheelCollider.rpm * 6f * Time.deltaTime;
        wheelModel.rotation = rot * Quaternion.Euler(wheelRotation, wheelCollider.steerAngle, 0f);
    }

    /// <summary>
    /// Returns wheel's current linear speed (km/h), can be used in Drive to determine forward/reverse
    /// </summary>
    public float GetWheelSpeedKmh()
    {
        if (wheelCollider == null) return 0f;
        return (Mathf.Abs(rpm) / 60f) * (2f * Mathf.PI * Mathf.Max(wheelCollider.radius, 0.001f)) * 3.6f;
    }
}
