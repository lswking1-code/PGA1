using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class DriveController : MonoBehaviour
{
    
    [Header("Attributes")]
    public float HP = 100;
    public float MaxHP = 100;
    public float Armor = 100;
    public float MaxArmor = 100;
    public float Gas = 100;
    public float MaxGas = 100;
    [Range(0, 1)]
    public float GasConsumption = 1f;

    
    [Header("Input")]
    public float gasInput;
    public float brakeInput;
    public Vector2 steeringInput;
    
    [Header("Physics")]
    public WheelCollider[] wheelColliders;
    public GameObject[] wheels;
    [Range(0, 2000)]
    public float driveTorque = 100;
    [Range(0, 500)]
    public float brakeTorque = 500;
    public float forwardTorque;

    public float Downforce = 100;

    public float SteeringAngle = 30;
    public bool isReverse = false;
    


    /*[Range(0, 100)]
    public float speed = 10f;
    [Range(0, 100)]
    public float turnSpeed = 100f;
    [Range(0, 100)]
    public float maxSpeed = 20f;
    [Range(0, 5)]
    public float navMeshSearchRadius = 2f;
    [Range(0, 1)]
    public float navMeshTolerance = 0.5f;
    [Range(0, 45)]
    public float maxSlopeAngle = 30f;
    */
    [Header("Anti-Rollover")]
    [Range(-2, 0)]
    public float centerOfMassY = -0.5f; // 重心Y偏移（负值降低重心）
    [Range(0, 10)]
    public float antiRolloverForce = 3f; // 防侧翻力强度
    [Range(0, 90)]
    public float maxTiltAngle = 45f; // 最大倾斜角度（超过此角度会纠正）
    

    private Rigidbody rb;
    private float horizontalInput;
    private float forwardInput;

    void Start()
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

    void Update()
    {
        AddDownforce();
    }

    void FixedUpdate()
    {
        // 应用防侧翻控制
        //PreventRollover();
        if(Gas > 0)
        {
            Drive(gasInput, brakeInput, steeringInput);
            Gas -= GasConsumption * Time.deltaTime;
        }
    }
    
    /*void PreventRollover()
    {
        if (rb == null) return;
        
        // 计算车辆当前倾斜角度
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        
        // 如果倾斜角度过大，应用纠正力
        if (tiltAngle > maxTiltAngle)
        {
            // 计算需要纠正的旋转
            Vector3 targetUp = Vector3.up;
            Vector3 currentUp = transform.up;
            
            // 计算旋转轴和角度
            Vector3 rotationAxis = Vector3.Cross(currentUp, targetUp).normalized;
            float rotationAngle = Vector3.Angle(currentUp, targetUp);
            
            // 应用纠正扭矩
            Vector3 correctionTorque = rotationAxis * rotationAngle * antiRolloverForce;
            rb.AddTorque(correctionTorque, ForceMode.Force);
        }
        
        // 限制X和Z轴的角速度，防止过度旋转
        Vector3 angularVelocity = rb.angularVelocity;
        angularVelocity.x = Mathf.Clamp(angularVelocity.x, -2f, 2f);
        angularVelocity.z = Mathf.Clamp(angularVelocity.z, -2f, 2f);
        rb.angularVelocity = angularVelocity;
    }*/

    void LateUpdate()
    {
        if (rb == null) return;
        //ConstrainToNavMesh();
    }

    /*private void ConstrainToNavMesh()
    {
        NavMeshHit hit = default;
        float[] searchRadii = { navMeshTolerance, navMeshSearchRadius, navMeshSearchRadius * 2f };
        
        // 尝试不同搜索半径找到 NavMesh
        bool foundNavMesh = false;
        foreach (float radius in searchRadii)
        {
            if (NavMesh.SamplePosition(transform.position, out hit, radius, NavMesh.AllAreas))
            {
                foundNavMesh = true;
                break;
            }
        }

        if (!foundNavMesh) return;

        float distance = Vector3.Distance(transform.position, hit.position);
        float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
        
        // 检查是否需要调整位置
        if (distance > navMeshTolerance + 0.2f && slopeAngle <= maxSlopeAngle)
        {
            Vector3 offset = hit.position - transform.position;
            float verticalDistance = Mathf.Abs(offset.y);
            float horizontalDistance = new Vector3(offset.x, 0, offset.z).magnitude;
            
            if (verticalDistance > 0.2f || horizontalDistance > 0.5f)
            {
                Vector3 newPosition = distance > 1f 
                    ? hit.position 
                    : Vector3.Lerp(transform.position, hit.position, Time.deltaTime * 10f);
                
                transform.position = newPosition;
                rb.position = newPosition;

                // 将速度投影到 NavMesh 表面
                if (rb.linearVelocity.magnitude > 0.1f)
                {
                    Vector3 projectedVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, hit.normal);
                    if (projectedVelocity.magnitude > 0.01f)
                    {
                        rb.linearVelocity = projectedVelocity.normalized * rb.linearVelocity.magnitude;
                    }
                }
            }
        }
    }*/
    public void OnAccelerate(InputValue button)
    {
        if(button.isPressed)
        {
            gasInput = 1;
        }
        else
        {
            gasInput = 0;
        }
    }
    public void OnBrake(InputValue button)
    {
        if(button.isPressed)
        {
            brakeInput = 1;
        }
        else
        {
            brakeInput = 0;
        }
    }
    public void OnSteering(InputValue value)
    {
        steeringInput = value.Get<Vector2>();
    }

    public void Drive(float acceleration, float brake, Vector2 steer)
    {
        // 获取当前速度（在车辆前进方向上的速度分量）
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        
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
    public void TakeDamage(float damage)
    {
        HP -= damage;
        if(HP <= 0)
        {
            Destroy(gameObject);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Chaser"))
        {
            TakeDamage(other.GetComponent<Chasing>().Damage);
        }
    }
}
