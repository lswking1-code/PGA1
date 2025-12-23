using UnityEngine;
using UnityEngine.AI;

public class Chasing : MonoBehaviour
{
    [Header("目标")]
    public Transform target;
    
    [Header("Speed")]
    [Range(0, 100)] public float moveSpeed = 5f;
    [Range(0, 100)] public float rotationSpeed = 5f;
    
    [Header("Player Interaction")]
    [Range(0, 1)] public float slowDownMultiplier = 0.5f; // 减速倍数（0.5 = 50%速度）
    public float slowDownDuration = 3f; // 减速持续时间（秒）
    [Range(0, 10)] public float keepDistance = 3f; // 减速时保持与玩家的距离
    
    [Header("Collision")]
    [Range(0, 1000)] public float pushForce = 100f; // 对Player施加的力的大小
    [Range(0, 100)] public float collisionForce = 10f; // 对Chaser施加的力的大小
    [Range(0, 1000)] public float recoilForce = 50f; // 撞击玩家后敌人受到的向后力的大小
    
    private NavMeshAgent agent;
    private Rigidbody rb; // Rigidbody 组件，用于物理移动
    private float currentMoveSpeed; // 当前移动速度
    private bool isSlowedDown; // 是否处于减速状态
    private Coroutine recoverCoroutine; // 恢复速度的协程引用

    public GameObject[] wheels;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        
        // 禁用 NavMeshAgent 的自动移动，我们将手动控制
        agent.updatePosition = false;
        agent.updateRotation = false;
        
        // 初始化当前移动速度
        currentMoveSpeed = moveSpeed;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        // 使用 NavMeshAgent 计算路径
        agent.SetDestination(target.position);
        
        // 检查路径是否有效
        if (agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            // 如果路径无效，尝试找到最近的有效 NavMesh 位置
            SnapToNavMesh();
            return;
        }

        // 检查是否在减速状态且需要保持距离
        if (ShouldKeepDistance()) return;

        // 获取路径的下一个点并移动
        if (agent.path.corners.Length > 1)
        {
            MoveTowards(agent.path.corners[1]);
        }
    }

    // 检查是否在减速状态且距离玩家太近
    bool ShouldKeepDistance()
    {
        if (!isSlowedDown) return false;
        
        float distance = Vector3.Distance(transform.position, target.position);
        // 如果距离太近，停止移动，只转向
        if (distance < keepDistance)
        {
            LookAt(target.position);
            return true;
        }
        return false;
    }

    // 朝目标点移动
    void MoveTowards(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position);
        direction.y = 0; // 忽略垂直方向
        
        if (direction.magnitude < 0.1f) return;

        // 转向目标点
        LookAt(targetPoint);

        // 计算新位置（使用当前速度，可能已被减速）
        Vector3 newPosition = rb.position + transform.forward * currentMoveSpeed * Time.fixedDeltaTime;
        
        // 在减速状态下，检查移动后是否会太靠近玩家
        if (isSlowedDown && Vector3.Distance(newPosition, target.position) < keepDistance)
            return;

        // 检查新位置是否在 NavMesh 上
        if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            // 如果新位置在 NavMesh 上，使用 NavMesh 上的位置
            rb.MovePosition(hit.position);
            agent.nextPosition = rb.position;
        }
        else
        {
            // 如果新位置不在 NavMesh 上，使用当前位置到 NavMesh 的投影
            SnapToNavMesh();
        }
    }

    // 转向目标位置
    void LookAt(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0; // 忽略垂直方向
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // 将位置投影到最近的 NavMesh 位置
    void SnapToNavMesh()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            rb.position = hit.position;
            agent.nextPosition = hit.position;
        }
    }

    private void RotateWheels()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].transform.Rotate(-10, 0, 0);
        }
    }    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PlayerCollision(other.gameObject);
        else if (other.CompareTag("Chaser"))
            ChaserCollision(other.gameObject);
    }

    // 处理与其他 Chaser 的碰撞
    void ChaserCollision(GameObject chaser)
    {
        Rigidbody chaserRb = chaser.GetComponent<Rigidbody>();
        if (chaserRb != null)
        {
            Vector3 force = (chaser.transform.position - transform.position).normalized;
            force.y = 0;
            chaserRb.AddForce(force * collisionForce, ForceMode.Impulse);
        }
    }

    // 处理与Player的碰撞
    void PlayerCollision(GameObject player)
    {
        // 如果还没有减速，则开始减速
        if (!isSlowedDown)
        {
            isSlowedDown = true;
            currentMoveSpeed = moveSpeed * slowDownMultiplier;
        }
        
        // 计算力的方向（从玩家指向敌人）
        Vector3 forceDirection = (player.transform.position - transform.position).normalized;
        forceDirection.y = 0;
        
        // 对Player施加力
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.AddForce(forceDirection * pushForce, ForceMode.Impulse);
        }
        
        // 对敌人自身施加向后的力（与玩家受到的力方向相反）
        if (rb != null)
        {
            Vector3 recoilDirection = -forceDirection; // 向后方向
            rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);
        }
        
        // 停止之前的协程（如果存在）并重新开始计时
        if (recoverCoroutine != null)
            StopCoroutine(recoverCoroutine);
        recoverCoroutine = StartCoroutine(RecoverSpeedAfterDelay());
    }
    
    // 协程：延迟后恢复速度
    System.Collections.IEnumerator RecoverSpeedAfterDelay()
    {
        yield return new WaitForSeconds(slowDownDuration);
        currentMoveSpeed = moveSpeed;
        isSlowedDown = false;
    }
}
