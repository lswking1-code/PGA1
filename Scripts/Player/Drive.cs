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
    public float centerOfMassY = -0.5f; // 重心Y偏移（负值降低重心）
    [Range(0, 10)]
    public float antiRolloverForce = 3f; // 防侧翻力强度
    [Range(0, 90)]
    public float maxTiltAngle = 45f; // 最大倾斜角度（超过此角度会纠正）

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 配置 Rigidbody 以防止侧翻
        if (rb != null)
        {
            // 降低重心，防止侧翻
            rb.centerOfMass = new Vector3(0, centerOfMassY, 0);
            
            // 增加角阻力，减少旋转
            rb.angularDamping = 5f;
        }
    }
    private void Update()
    {
        AddDownforce();
    }

    public void Driving(float acceleration, float brake, Vector2 steer)
    {
        // 获取当前速度（在车辆前进方向上的速度分量）
        //float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float currentSpeed = GetCurrentSpeed();
        // 前进
        forwardTorque = acceleration * driveTorque;
        
        // 如果速度 <= 0 且按住刹车，则倒车
        if (currentSpeed <= 0 && brake > 0)
        {
            // 应用反向扭矩（倒车）
            forwardTorque = -brake * driveTorque;
            brake = 0; // 倒车时不应用刹车扭矩
            isReverse = true;
        }
        else
        {
            // 正常情况：应用刹车扭矩
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
