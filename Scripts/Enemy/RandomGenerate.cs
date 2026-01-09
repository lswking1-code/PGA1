using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class RandomGenerate : MonoBehaviour
{
    [Header("EventListeners")]
    public VoidEventSO AfterSceneLoadedEvent;

    public GameObject enemyPrefabs;
    public Transform playerTransform;
    public Transform[] generatePoints;
    
    [Header("Generation Settings")]
    public float checkInterval = 60f; // 检测间隔（秒），默认60秒（1分钟）
    public int minEnemyCount = 3; // 最小敌人数量
    

    private void OnEnable()
    {
        AfterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadedEvent;
    }
    private void OnDisable()
    {
        AfterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
    }
    private void Start()
    {
        // 启动协程，每分钟检测一次
        StartCoroutine(CheckAndGenerateEnemies());
        FindPlayer();
    }
    private void Update()
    {
        FindPlayer();
    }
    /// <summary>
    /// 协程：每分钟检测场景中Tag为"Chaser"的物体数量，如果小于3则生成敌人
    /// </summary>
    private IEnumerator CheckAndGenerateEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            // 检测场景中Tag为"Chaser"的物体数量
            GameObject[] chasers = GameObject.FindGameObjectsWithTag("Chaser");
            int chaserCount = chasers.Length;
            
            Debug.Log($"当前场景中Chaser数量: {chaserCount}");
            
            // 如果数量小于3，则生成敌人
            if (chaserCount < minEnemyCount)
            {
                // 找到距离玩家最近的生成点
                Transform nearestPoint = FindNearestGeneratePoint();
                
                if (nearestPoint != null && enemyPrefabs != null)
                {
                    // 在最近的位置生成敌人
                    Instantiate(enemyPrefabs, nearestPoint.position, nearestPoint.rotation);
                    Debug.Log($"在位置 {nearestPoint.position} 生成了新的敌人");
                }
                else
                {
                    if (nearestPoint == null)
                        Debug.LogWarning("没有找到可用的生成点！");
                    if (enemyPrefabs == null)
                        Debug.LogWarning("敌人预制体未设置！");
                }
            }
        }
    }
    
    /// <summary>
    /// 找到距离玩家最近的生成点
    /// </summary>
    private Transform FindNearestGeneratePoint()
    {
        if (playerTransform == null || generatePoints == null || generatePoints.Length == 0)
            return null;
        
        Transform nearestPoint = generatePoints[0];
        float nearestDistance = Vector3.Distance(playerTransform.position, nearestPoint.position);
        
        // 遍历所有生成点，找到距离玩家最近的一个
        for (int i = 1; i < generatePoints.Length; i++)
        {
            if (generatePoints[i] == null)
                continue;
                
            float distance = Vector3.Distance(playerTransform.position, generatePoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = generatePoints[i];
            }
        }
        
        return nearestPoint;
    }
        /// <summary>
    /// 查找Player对象（支持跨场景查找）
    /// </summary>
    private void FindPlayer()
    {
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
                        playerTransform = obj.transform;
                        return;
                    }
                }
            }
        }
        
        // 如果还是没找到，输出警告
        if (playerTransform == null)
        {
            Debug.LogWarning("RandomGenerate: Tag Player not found in any loaded scene");
        }
    }
    private void OnAfterSceneLoadedEvent()
    {
        // 清空所有生成的enemyPrefabs（Tag为"Chaser"的对象）
        GameObject[] chasers = GameObject.FindGameObjectsWithTag("Chaser");
        foreach (GameObject chaser in chasers)
        {
            if (chaser != null)
            {
                Destroy(chaser);
            }
        }
        
        Debug.Log($"场景加载后清空了 {chasers.Length} 个敌人");
    }
}
