using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Chase : MonoBehaviour
{
    private Transform targetTransform;
    private NavMeshAgent agent;
    private Drive drive;
    public Vector3 targetPosition;
    private float currentSpeed = 10f;
    [Range(0, 100)]
    public float brakeSpeed = 15f;
    
    [Header("Distance Settings")]
    [Range(0, 100)]
    public float reverseDistance = 25;
    [Range(0, 100)]
    public float stoppingDistance = 30;
    [Range(0, 100)]
    public float stoppingSpeed = 50;
    
    [Header("Steering Settings")]
    [Range(1, 20)]
    public float steeringSmoothing = 10f; // 转向平滑速度（值越大越平滑）
    [Range(0, 30)]
    public float maxSteeringAngle = 30f; // 最大转向角度
    public float speedBasedSteeringMultiplier = 0.5f; // 速度对转向的影响
    
    private float currentTurnAmount = 0f; // 当前转向值（用于平滑）
    
    private void Start()
    {
        drive = GetComponent<Drive>();
        agent = GetComponent<NavMeshAgent>();
        //禁用NavMeshAgent的自动移动
        agent.updatePosition = false;
        agent.updateRotation = false;
        
        // 通过Tag查找Player对象（支持跨场景查找）
        FindPlayer();
    }
    private void Update()
    { 
        FindPlayer();
        
        if (targetTransform != null && agent != null)
        {
            // 使用 NavMeshAgent 计算路径
            agent.SetDestination(targetTransform.position);
        }
    }

    private void FixedUpdate()
    {
        ChaseAI();
    }
    public void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }
    public void ChaseAI()
    {
        if (targetTransform == null || agent == null) return;

        // 检查路径是否有效
        if (agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            // 如果路径无效，尝试找到最近的有效 NavMesh 位置
            SnapToNavMesh();
            return;
        }

        // 获取导航目标位置（使用路径的下一个点，如果没有路径点则使用最终目标）
        Vector3 navigationTarget = targetTransform != null ? targetTransform.position : transform.position;
        if (agent.path.corners.Length > 1)
        {
            navigationTarget = agent.path.corners[1]; // 使用路径的下一个点
        }
        else if (agent.path.corners.Length > 0)
        {
            navigationTarget = agent.path.corners[agent.path.corners.Length - 1]; // 使用路径的最后一个点
        }
        else
        {
            navigationTarget = targetTransform.position; // 如果没有路径，使用目标位置
        }

        float forwardAmount = 0;
        float brakeAmount = 0;
        float targetTurnAmount = 0;
 
        currentSpeed = drive.GetCurrentSpeed();

        float reachedTargetDistance = 7f;

        float distanceToTarget = Vector3.Distance(transform.position, navigationTarget);
        
        if(distanceToTarget > reachedTargetDistance)
        {
            Vector3 dirToMovePosition = (navigationTarget - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);

            if(dot > 0)//前进判断
            {
                forwardAmount = 1;
                brakeAmount = 0;
                //靠近时减速
                if(distanceToTarget < stoppingDistance && currentSpeed > stoppingSpeed)
                {
                    forwardAmount = 0;
                    brakeAmount = 1;
                }
            }
            else
            {
                if(distanceToTarget > reverseDistance)
                {
                    forwardAmount = 1;
                }
                else
                {
                    forwardAmount = 0;
                    brakeAmount = 1;
                }
            }

            float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
            
            // 根据角度计算转向强度（-1 到 1），而不是直接设为 1 或 -1
            // 角度越大，转向强度越大，但最大不超过 1
            float normalizedAngle = Mathf.Clamp(angleToDir / maxSteeringAngle, -1f, 1f);//AI
            targetTurnAmount = normalizedAngle;//AI
            
        }
        else
        {
            if(currentSpeed > brakeSpeed)//减速判断
            {
                brakeAmount = 1;
            }
            else
            {
                forwardAmount = 0;//减速到15f以下时，停止前进并刹车
                brakeAmount = 0;
            }
            targetTurnAmount = 0;//减速到15f以下时，停止转向并刹车
        }
        
        // 平滑转向值，避免突然变化（使用基于时间的插值）
        currentTurnAmount = Mathf.Lerp(currentTurnAmount, targetTurnAmount, steeringSmoothing * Time.fixedDeltaTime);//AI
        
        drive.Driving(forwardAmount, brakeAmount, new Vector2(currentTurnAmount, 0));
        
        // 更新 agent 的位置以保持同步
        agent.nextPosition = transform.position;
    }
    /// <summary>
    /// 查找Player对象（支持跨场景查找）
    /// </summary>
    private void FindPlayer()//AI
    {
        // 方法2: 遍历所有加载的场景查找Player
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    if (obj.CompareTag("Player"))
                    {
                        targetTransform = obj.transform;
                        return;
                    }
                    // 递归查找子对象
                    Transform found = FindInChildren(obj.transform, "Player");
                    if (found != null)
                    {
                        targetTransform = found;
                        return;
                    }
                }
            }
        }
        
        // 如果还是没找到，输出警告
        if (targetTransform == null)
        {
            Debug.LogWarning("Chase: Tag Player not found in any loaded scene");
        }
    }
    
    /// <summary>
    /// 在子对象中递归查找指定Tag的对象
    /// </summary>
    private Transform FindInChildren(Transform parent, string tag)//AI
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
            Transform found = FindInChildren(child, tag);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    
    //TODO:优化Nav逻辑 BUG原因：敌人遇到障碍物并且玩家距离过远时，敌人会卡在障碍物上
    // 将位置投影到最近的 NavMesh 位置
    private void SnapToNavMesh()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            if (agent != null)
            {
                agent.nextPosition = hit.position;
            }
        }
    }

}
