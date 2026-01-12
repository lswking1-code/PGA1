using UnityEngine;

public class Drive : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Physics")]
    public WheelCollider[] wheelColliders;
    public GameObject[] wheels;
    [Range(0, 2000)]
    public float driveTorque = 100;
    [Range(0, 1000)]
    public float brakeTorque = 500;
    public float forwardTorque;

    public float Downforce = 100;

    public float SteeringAngle = 30;
    public bool isReverse = false;
    private Rigidbody rb;

    [Header("Anti-Rollover")]
    [Range(-2, 0)]
    public float centerOfMassY = -0.5f; // Center of mass Y offset (negative value lowers center of mass)
    [Range(0, 10)]
    public float antiRolloverForce = 3f; // Anti-rollover force strength
    [Range(0, 90)]
    public float maxTiltAngle = 45f; // Maximum tilt angle (will correct if exceeded)

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Configure Rigidbody to prevent rollover
        if (rb != null)
        {
            // Lower center of mass to prevent rollover
            rb.centerOfMass = new Vector3(0, centerOfMassY, 0);
            
            // Increase angular damping to reduce rotation
            rb.angularDamping = 5f;
        }
    }
    private void Update()
    {
        AddDownforce();
    }

    public void Driving(float acceleration, float brake, Vector2 steer)
    {
        // Get current speed (velocity component in vehicle forward direction)
        //float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float currentSpeed = GetCurrentSpeed();
        // Forward
        forwardTorque = acceleration * driveTorque;
        
        // If speed <= 0 and brake is held, reverse
        if (currentSpeed <= 0 && brake > 0)
        {
            // Apply reverse torque (reversing)
            forwardTorque = -brake * driveTorque;
            brake = 0; // Don't apply brake torque when reversing
            isReverse = true;
        }
        else
        {
            // Normal case: apply brake torque
            brake *= brakeTorque;
            isReverse = false;
        }
        
        steer.x = steer.x * SteeringAngle;
        /*for (int i = 0; i < wheels.Length; i++)
        {
            Vector3 wheelPosition;
            Quaternion wheelRotation;

            wheelColliders[i].GetWorldPose(out wheelPosition, out wheelRotation);
            wheels[i].transform.position = wheelPosition;
            wheels[i].transform.rotation = wheelRotation;
        }*/

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            wheelColliders[i].motorTorque = forwardTorque;
            wheelColliders[i].brakeTorque = brake;
            if(i < 2)
            {
                wheelColliders[i].steerAngle = steer.x;
            }
        }
    }
    public void AddDownforce()
    {
        wheelColliders[0].attachedRigidbody.AddForce(-transform.up * Downforce * wheelColliders[0].attachedRigidbody.linearVelocity.magnitude);
    }
    public float GetCurrentSpeed()
    {
        return Vector3.Dot(rb.linearVelocity, transform.forward);
    }
}
