using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    [Header("EventRaise")]
    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;

    private List<ISaveable> saveableList = new List<ISaveable>();

    private Data saveData;

    private string jsonFolder;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

        saveData = new Data();

        jsonFolder = Application.persistentDataPath + "/SAVE DATA/";

        ReadSavedData();
    }

    private void OnEnable()
    {
        if (saveDataEvent != null)
            saveDataEvent.OnEventRaised += Save;
        if (loadDataEvent != null)
            loadDataEvent.OnEventRaised += Load;
    }

    private void OnDisable()
    {
        if (saveDataEvent != null)
            saveDataEvent.OnEventRaised -= Save;
        if (loadDataEvent != null)
            loadDataEvent.OnEventRaised -= Load;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
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

        // 同步字典到列表以便序列化
        saveData.SyncDictionariesToLists();

        var resultPath = jsonFolder + "data.sav";

        // 使用 Unity 的 JsonUtility 序列化
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

            // 使用 Unity 的 JsonUtility 反序列化
            saveData = JsonUtility.FromJson<Data>(stringData);
            
            if (saveData == null)
            {
                saveData = new Data();
            }
            else
            {
                // 从列表重建字典
                saveData.InitializeDictionariesFromLists();
            }
        }
    }
}
