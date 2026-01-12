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
    public float checkInterval = 60f; // Check interval (in seconds), default 60 seconds (1 minute)
    public int minEnemyCount = 3; // Minimum enemy count
    

    private void OnEnable()
    {
        if (AfterSceneLoadedEvent != null) AfterSceneLoadedEvent.AddListener(OnAfterSceneLoadedEvent);
    }
    private void OnDisable()
    {
        if (AfterSceneLoadedEvent != null) AfterSceneLoadedEvent.RemoveListener(OnAfterSceneLoadedEvent);
    }
    private void Start()
    {
        // Start coroutine to check every minute
        StartCoroutine(CheckAndGenerateEnemies());
        FindPlayer();
    }
    private void Update()
    {
        FindPlayer();
    }
    /// <summary>
    /// Coroutine: Check the number of objects with tag "Chaser" in the scene every minute, generate enemies if less than 3
    /// </summary>
    private IEnumerator CheckAndGenerateEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            // Check the number of objects with tag "Chaser" in the scene
            GameObject[] chasers = GameObject.FindGameObjectsWithTag("Chaser");
            int chaserCount = chasers.Length;
            
            Debug.Log($"Current Chaser count in scene: {chaserCount}");
            
            // If count is less than 3, generate enemies
            if (chaserCount < minEnemyCount)
            {
                // Find the spawn point nearest to the player
                Transform nearestPoint = FindNearestGeneratePoint();
                
                if (nearestPoint != null && enemyPrefabs != null)
                {
                    // Spawn enemy at the nearest position
                    Instantiate(enemyPrefabs, nearestPoint.position, nearestPoint.rotation);
                    Debug.Log($"Spawned new enemy at position {nearestPoint.position}");
                }
                else
                {
                    if (nearestPoint == null)
                        Debug.LogWarning("No available spawn point found!");
                    if (enemyPrefabs == null)
                        Debug.LogWarning("Enemy prefab is not set!");
                }
            }
        }
    }
    
    /// <summary>
    /// Find the spawn point nearest to the player
    /// </summary>
    private Transform FindNearestGeneratePoint()
    {
        if (playerTransform == null || generatePoints == null || generatePoints.Length == 0)
            return null;
        
        Transform nearestPoint = generatePoints[0];
        float nearestDistance = Vector3.Distance(playerTransform.position, nearestPoint.position);
        
        // Iterate through all spawn points to find the one nearest to the player
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
    /// Find Player object (supports cross-scene lookup)
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
        
        // If still not found, output warning
        if (playerTransform == null)
        {
            Debug.LogWarning("RandomGenerate: Tag Player not found in any loaded scene");
        }
    }
    private void OnAfterSceneLoadedEvent()
    {
        // Clear all spawned enemyPrefabs (objects with tag "Chaser")
        GameObject[] chasers = GameObject.FindGameObjectsWithTag("Chaser");
        foreach (GameObject chaser in chasers)
        {
            if (chaser != null)
            {
                Destroy(chaser);
            }
        }
        
        Debug.Log($"Cleared {chasers.Length} enemies after scene loaded");
    }
}
