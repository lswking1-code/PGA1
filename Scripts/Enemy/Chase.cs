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
    
    [Header("EventListeners")]
    public VoidEventSO TimeoutEvent;
    public VoidEventSO GameOverEvent;
    [Header("Wall Detection Settings")]
    [Range(0, 50)]
    public float wallDetectionDistance = 10f; // 射线检测距离
    public bool isWall = false;
    public float wallDistance = 10f; // 与Wall的距离
    public float reverseWallDistance = 10;
    [Range(0, 5)]
    public float reverseSpeedThreshold = 1f; // 速度阈值，低于此值可以倒车
    
    [Header("Ray Position Offsets")]
    public Vector3 ray1Offset1 = Vector3.zero;
    public Vector3 ray1Offset2 = Vector3.zero;
    public Vector3 ray2Offset1 = Vector3.zero;
    public Vector3 ray2Offset2 = Vector3.zero;  
    
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
    private bool isReversing = false; // 是否正在倒车状态
    private float reversingStartTime = 0f; // 开始倒车的时间
    [Range(0, 5)]
    public float forceReverseTime = 1f; // 强制倒车的等待时间
    private void OnEnable()
    {
        TimeoutEvent.OnEventRaised += OnTimeoutEvent;
        GameOverEvent.OnEventRaised += OnGameOverEvent;
    }
    private void OnDisable()
    {
        TimeoutEvent.OnEventRaised -= OnTimeoutEvent;
        GameOverEvent.OnEventRaised -= OnGameOverEvent;
    }
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
        CheckForWall();
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
        
        // 优先检查倒车逻辑（无论车辆朝向如何，无论距离如何）
        // 如果检测到墙，开始倒车状态
        if(isWall)//TODO：如果两面都是墙，依然会卡死
        {
            if(!isReversing)
            {
                // 刚开始倒车状态，记录时间
                isReversing = true;
                reversingStartTime = Time.time;
            }
        }
        
        // 如果正在倒车状态，持续倒车直到距离足够远
        if(isReversing)
        {
            if(wallDistance > reverseWallDistance)
            {
                // 距离足够远，结束倒车状态
                isReversing = false; 
                reversingStartTime = 0f;
            }
            else
            {
                // 检查是否应该强制倒车（距离很近且等待时间足够）
                bool shouldForceReverse = wallDistance < 2f && (Time.time - reversingStartTime) > forceReverseTime;
                
                // 继续倒车：先刹车直到速度降到阈值以下，然后倒车
                if(currentSpeed > reverseSpeedThreshold && !shouldForceReverse)
                {
                    // 速度还太高，先刹车（除非需要强制倒车）
                    forwardAmount = 0;
                    brakeAmount = 1;
                    targetTurnAmount = 0;
                }
                else
                {
                    // 速度足够低或需要强制倒车，可以倒车
                    forwardAmount = 0;
                    brakeAmount = 1; // Drive类会在速度<=0时自动转换为倒车
                    targetTurnAmount = 0; // 倒车时不转向
                }
            }
        }
        
        // 如果不在倒车状态，执行正常的追逐逻辑
        if(!isReversing)
        {
            if(distanceToTarget > reachedTargetDistance)
            {
                Vector3 dirToMovePosition = (navigationTarget - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToMovePosition);

                // 正常判断前进或倒车
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
                    if(distanceToTarget > reverseDistance)//TODO:优化倒车逻辑
                    {
                        // 正常前进
                        forwardAmount = 1;
                        brakeAmount = 0;
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
                }
            }
        }
        
        // 如果还是没找到，输出警告
        if (targetTransform == null)
        {
            Debug.LogWarning("Chase: Tag Player not found in any loaded scene");
        }
    }
    
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
    
    /// <summary>
    /// 检测车辆前方是否有Tag为"Wall"的物体，并返回距离
    /// 使用两组射线，每组两根：第一组用于检测Wall（isWall），第二组用于获取距离（wallDistance）
    /// </summary>
    private void CheckForWall()
    {
        Vector3 basePosition = transform.position;
        Vector3 rayDirection = transform.forward;
        
        // 计算第一组两根射线的起点（应用位置偏移）
        Vector3 ray1Origin1 = basePosition + transform.TransformDirection(ray1Offset1);
        Vector3 ray1Origin2 = basePosition + transform.TransformDirection(ray1Offset2);
        
        // 第一组射线：用于检测Wall并设置isWall（如果任意一根检测到，isWall = true）
        bool wallDetected1 = false;
        bool wallDetected2 = false;
        
        RaycastHit hitForWall1;
        if (Physics.Raycast(ray1Origin1, rayDirection, out hitForWall1, wallDetectionDistance))
        {
            if (hitForWall1.collider.CompareTag("Wall"))
            {
                wallDetected1 = true;
                Debug.DrawRay(ray1Origin1, rayDirection * hitForWall1.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(ray1Origin1, rayDirection * hitForWall1.distance, Color.yellow);
            }
        }
        else
        {
            Debug.DrawRay(ray1Origin1, rayDirection * wallDetectionDistance, Color.green);
        }
        
        RaycastHit hitForWall2;
        if (Physics.Raycast(ray1Origin2, rayDirection, out hitForWall2, wallDetectionDistance))
        {
            if (hitForWall2.collider.CompareTag("Wall"))
            {
                wallDetected2 = true;
                Debug.DrawRay(ray1Origin2, rayDirection * hitForWall2.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(ray1Origin2, rayDirection * hitForWall2.distance, Color.yellow);
            }
        }
        else
        {
            Debug.DrawRay(ray1Origin2, rayDirection * wallDetectionDistance, Color.green);
        }
        
        // 如果任意一根检测到Wall，设置isWall为true
        isWall = wallDetected1 || wallDetected2;
        
        // 第二组射线：专门用于检测Wall并获取距离（wallDistance）
        // 计算第二组两根射线的起点（应用位置偏移）
        Vector3 ray2Origin1 = basePosition + transform.TransformDirection(ray2Offset1);
        Vector3 ray2Origin2 = basePosition + transform.TransformDirection(ray2Offset2);
        
        // 使用更长的检测距离以确保能检测到Wall
        float maxDistance = Mathf.Max(wallDetectionDistance, reverseWallDistance + 1);
        
        float minDistance = float.MaxValue;
        bool foundWall = false;
        
        RaycastHit hitForDistance1;
        if (Physics.Raycast(ray2Origin1, rayDirection, out hitForDistance1, maxDistance))
        {
            if (hitForDistance1.collider.CompareTag("Wall"))
            {
                minDistance = Mathf.Min(minDistance, hitForDistance1.distance);
                foundWall = true;
                Debug.DrawRay(ray2Origin1, rayDirection * hitForDistance1.distance, Color.blue);
            }
            else
            {
                Debug.DrawRay(ray2Origin1, rayDirection * hitForDistance1.distance, Color.cyan);
            }
        }
        else
        {
            Debug.DrawRay(ray2Origin1, rayDirection * maxDistance, Color.white);
        }
        
        RaycastHit hitForDistance2;
        if (Physics.Raycast(ray2Origin2, rayDirection, out hitForDistance2, maxDistance))
        {
            if (hitForDistance2.collider.CompareTag("Wall"))
            {
                minDistance = Mathf.Min(minDistance, hitForDistance2.distance);
                foundWall = true;
                Debug.DrawRay(ray2Origin2, rayDirection * hitForDistance2.distance, Color.blue);
            }
            else
            {
                Debug.DrawRay(ray2Origin2, rayDirection * hitForDistance2.distance, Color.cyan);
            }
        }
        else
        {
            Debug.DrawRay(ray2Origin2, rayDirection * maxDistance, Color.white);
        }
        
        // 设置wallDistance：如果检测到Wall，使用最近距离；否则设为倒车距离+1
        if (foundWall)
        {
            wallDistance = minDistance;
        }
        else
        {
            wallDistance = reverseWallDistance + 1;
        }
    }
    private void OnTimeoutEvent()
    {
        //agent.enabled = false;
    }
    private void OnGameOverEvent()
    {
        //agent.enabled = false;
    }
    public void ChaseDie()
    {
        Destroy(gameObject);
    }
}
