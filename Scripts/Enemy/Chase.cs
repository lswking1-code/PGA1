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
    public float wallDetectionDistance = 10f; // Raycast detection distance
    public bool isWall = false;
    public float wallDistance = 10f; // Distance to Wall
    public float reverseWallDistance = 10;
    [Range(0, 5)]
    public float reverseSpeedThreshold = 1f; // Speed threshold, can reverse if below this value
    
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
    public float steeringSmoothing = 10f; // Steering smoothing speed (higher values are smoother)
    [Range(0, 30)]
    public float maxSteeringAngle = 30f; // Maximum steering angle
    public float speedBasedSteeringMultiplier = 0.5f; // Speed's effect on steering
    
    private float currentTurnAmount = 0f; // Current turn value (for smoothing)
    private bool isReversing = false; // Whether currently in reverse state
    private float reversingStartTime = 0f; // Time when reversing started
    [Range(0, 5)]
    public float forceReverseTime = 1f; // Wait time for forced reverse
    private void OnEnable()
    {
        if (TimeoutEvent != null) TimeoutEvent.AddListener(OnTimeoutEvent);
        if (GameOverEvent != null) GameOverEvent.AddListener(OnGameOverEvent);
    }
    private void OnDisable()
    {
        if (TimeoutEvent != null) TimeoutEvent.RemoveListener(OnTimeoutEvent);
        if (GameOverEvent != null) GameOverEvent.RemoveListener(OnGameOverEvent);
    }
    private void Start()
    {
        drive = GetComponent<Drive>();
        agent = GetComponent<NavMeshAgent>();
        // Disable NavMeshAgent's automatic movement
        agent.updatePosition = false;
        agent.updateRotation = false;
        
        // Find Player object by tag (supports cross-scene lookup)
        FindPlayer();
    }
    private void Update()
    { 
        FindPlayer();
        
        if (targetTransform != null && agent != null)
        {
            // Use NavMeshAgent to calculate path
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

        // Check if path is valid
        if (agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            // If path is invalid, try to find nearest valid NavMesh position
            SnapToNavMesh();
            return;
        }

        // Get navigation target position (use next point in path, or final target if no path points)
        Vector3 navigationTarget = targetTransform != null ? targetTransform.position : transform.position;
        if (agent.path.corners.Length > 1)
        {
            navigationTarget = agent.path.corners[1]; // Use next point in path
        }
        else if (agent.path.corners.Length > 0)
        {
            navigationTarget = agent.path.corners[agent.path.corners.Length - 1]; // Use last point in path
        }
        else
        {
            navigationTarget = targetTransform.position; // If no path, use target position
        }

        float forwardAmount = 0;
        float brakeAmount = 0;
        float targetTurnAmount = 0;
 
        currentSpeed = drive.GetCurrentSpeed();

        float reachedTargetDistance = 7f;

        float distanceToTarget = Vector3.Distance(transform.position, navigationTarget);
        
        // Prioritize reverse logic (regardless of vehicle orientation or distance)
        // If wall is detected, start reverse state
        if(isWall)//TODO: Still gets stuck if both sides are walls
        {
            if(!isReversing)
            {
                // Just started reverse state, record time
                isReversing = true;
                reversingStartTime = Time.time;
            }
        }
        
        // If in reverse state, continue reversing until distance is far enough
        if(isReversing)
        {
            if(wallDistance > reverseWallDistance)
            {
                // Distance is far enough, end reverse state
                isReversing = false; 
                reversingStartTime = 0f;
            }
            else
            {
                // Check if should force reverse (very close and wait time sufficient)
                bool shouldForceReverse = wallDistance < 2f && (Time.time - reversingStartTime) > forceReverseTime;
                
                // Continue reversing: brake until speed drops below threshold, then reverse
                if(currentSpeed > reverseSpeedThreshold && !shouldForceReverse)
                {
                    // Speed still too high, brake first (unless forced reverse needed)
                    forwardAmount = 0;
                    brakeAmount = 1;
                    targetTurnAmount = 0;
                }
                else
                {
                    // Speed low enough or forced reverse needed, can reverse
                    forwardAmount = 0;
                    brakeAmount = 1; // Drive class will automatically convert to reverse when speed <= 0
                    targetTurnAmount = 0; // Don't steer when reversing
                }
            }
        }
        
        // If not in reverse state, execute normal chase logic
        if(!isReversing)
        {
            if(distanceToTarget > reachedTargetDistance)
            {
                Vector3 dirToMovePosition = (navigationTarget - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToMovePosition);

                // Normal forward/reverse judgment
                if(dot > 0)//Forward judgment
                {
                    forwardAmount = 1;
                    brakeAmount = 0;
                    // Slow down when close
                    if(distanceToTarget < stoppingDistance && currentSpeed > stoppingSpeed)
                    {
                        forwardAmount = 0;
                        brakeAmount = 1;
                    }
                }
                else
                {
                    if(distanceToTarget > reverseDistance)//TODO: Optimize reverse logic
                    {
                        // Normal forward
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
                
                // Calculate steering strength based on angle (-1 to 1), instead of directly setting to 1 or -1
                // Larger angle means stronger steering, but max is 1
                float normalizedAngle = Mathf.Clamp(angleToDir / maxSteeringAngle, -1f, 1f);//AI
                targetTurnAmount = normalizedAngle;//AI
                
            }
            else
            {
                if(currentSpeed > brakeSpeed)//Deceleration judgment
                {
                    brakeAmount = 1;
                }
                else
                {
                    forwardAmount = 0;//When slowed to below 15f, stop forward and brake
                    brakeAmount = 0;
                }
                targetTurnAmount = 0;//When slowed to below 15f, stop steering and brake
            }
        }
        
        // Smooth turn value to avoid sudden changes (using time-based interpolation)
        currentTurnAmount = Mathf.Lerp(currentTurnAmount, targetTurnAmount, steeringSmoothing * Time.fixedDeltaTime);//AI
        
        drive.Driving(forwardAmount, brakeAmount, new Vector2(currentTurnAmount, 0));
        
        // Update agent position to keep in sync
        agent.nextPosition = transform.position;
    }
    /// <summary>
    /// Find Player object (supports cross-scene lookup)
    /// </summary>
    private void FindPlayer()//AI
    {
        // Method 2: Iterate through all loaded scenes to find Player
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
        
        // If still not found, output warning
        if (targetTransform == null)
        {
            Debug.LogWarning("Chase: Tag Player not found in any loaded scene");
        }
    }
    
    // Project position to nearest NavMesh position
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
    /// Detect if there is an object with tag "Wall" in front of the vehicle and return distance
    /// Uses two groups of rays, two rays each: first group detects Wall (isWall), second group gets distance (wallDistance)
    /// </summary>
    private void CheckForWall()
    {
        Vector3 basePosition = transform.position;
        Vector3 rayDirection = transform.forward;
        
        // Calculate origins of first group's two rays (apply position offset)
        Vector3 ray1Origin1 = basePosition + transform.TransformDirection(ray1Offset1);
        Vector3 ray1Origin2 = basePosition + transform.TransformDirection(ray1Offset2);
        
        // First group of rays: detect Wall and set isWall (if any ray detects, isWall = true)
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
        
        // If any ray detects Wall, set isWall to true
        isWall = wallDetected1 || wallDetected2;
        
        // Second group of rays: specifically for detecting Wall and getting distance (wallDistance)
        // Calculate origins of second group's two rays (apply position offset)
        Vector3 ray2Origin1 = basePosition + transform.TransformDirection(ray2Offset1);
        Vector3 ray2Origin2 = basePosition + transform.TransformDirection(ray2Offset2);
        
        // Use longer detection distance to ensure Wall can be detected
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
        
        // Set wallDistance: if Wall is detected, use nearest distance; otherwise set to reverse distance + 1
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
