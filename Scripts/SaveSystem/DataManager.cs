using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

[DefaultExecutionOrder(-100)]
public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    [Header("EventRaise")]
    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;
    
    [Header("WebGL Profiler Settings")]
    [Tooltip("Disable Profiler on WebGL platform (recommended, as WebGL usually doesn't need Profiler)")]
    public bool disableProfilerInWebGL = true;
    [Tooltip("If not disabling Profiler, set maximum memory limit (MB)")]
    public int profilerMaxMemoryMB = 512;

    private List<ISaveable> saveableList = new List<ISaveable>();

    private Data saveData;

    private string jsonFolder;

    private void Awake()
    {
        // Handle Profiler issues on WebGL platform (must execute earliest)
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Check if it's a Development Build (Development Build automatically enables Profiler)
        if (Debug.isDebugBuild)
        {
            Debug.LogWarning("WebGL Development Build detected. Profiler may be enabled. Consider building a Release build to disable Profiler.");
        }
        
        if (disableProfilerInWebGL)
        {
            // Try multiple methods to disable Profiler
            try
            {
                // Method 1: Direct disable
                Profiler.enabled = false;
                
                // Method 2: Set memory limit to 0 (may work in some Unity versions)
                try
                {
                    Profiler.maxUsedMemory = 0;
                }
                catch { }
                
                Debug.Log("Profiler disabled for WebGL build");
            }
            catch (System.Exception e)
            {
                // If cannot disable, increase memory limit instead
                Debug.LogWarning($"Cannot disable Profiler: {e.Message}. Increasing memory limit instead.");
                try
                {
                    Profiler.maxUsedMemory = profilerMaxMemoryMB * 1024 * 1024;
                }
                catch (System.Exception e2)
                {
                    Debug.LogError($"Failed to set Profiler memory limit: {e2.Message}");
                }
            }
        }
        else
        {
            // Increase Profiler memory limit
            try
            {
                Profiler.maxUsedMemory = profilerMaxMemoryMB * 1024 * 1024;
                Debug.Log($"Profiler max memory set to {profilerMaxMemoryMB}MB");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Profiler memory limit: {e.Message}");
            }
        }
        #endif

        instance = this;

        saveData = new Data();

        jsonFolder = Application.persistentDataPath + "/SAVE DATA/";

        ReadSavedData();
    }

    private void OnEnable()
    {
        if (saveDataEvent != null) saveDataEvent.AddListener(Save);
        if (loadDataEvent != null) loadDataEvent.AddListener(Load);
    }

    private void OnDisable()
    {
        if (saveDataEvent != null) saveDataEvent.RemoveListener(Save);
        if (loadDataEvent != null) loadDataEvent.RemoveListener(Load);
    }

    private void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Load();
        }
    }

    public void RegisterSaveData(ISaveable saveable)
    {
        if (!saveableList.Contains(saveable))
        {
            saveableList.Add(saveable);
        }
    }

    public void UnRegisterSaveData(ISaveable saveable)
    {
        saveableList.Remove(saveable);
    }

    public void Save()//AI
    {
        foreach (var saveable in saveableList)
        {
            saveable.GetSaveData(saveData);
        }

        // Sync dictionary to list for serialization
        saveData.SyncDictionariesToLists();

        var resultPath = jsonFolder + "data.sav";

        // Serialize using Unity's JsonUtility
        var jsonData = JsonUtility.ToJson(saveData, true);

        if (!Directory.Exists(jsonFolder))
        {
            Directory.CreateDirectory(jsonFolder);
        }

        File.WriteAllText(resultPath, jsonData);
    }

    public void Load()
    {
        foreach (var saveable in saveableList)
        {
            saveable.LoadSaveData(saveData);
        }
    }

    private void ReadSavedData()//AI
    {
        var resultPath = jsonFolder + "data.sav";

        if (File.Exists(resultPath))
        {
            var stringData = File.ReadAllText(resultPath);

            // Deserialize using Unity's JsonUtility
            saveData = JsonUtility.FromJson<Data>(stringData);
            
            // Rebuild dictionary from list
            saveData.InitializeDictionariesFromLists();
        }
    }
}
