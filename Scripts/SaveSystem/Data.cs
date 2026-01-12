using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Data
{
    public string sceneToSave;

    // Use serializable lists to replace Dictionary (JsonUtility doesn't support Dictionary)
    public List<KeyValuePairStringVector3> characterPosList = new List<KeyValuePairStringVector3>();
    public List<KeyValuePairStringFloat> floatSavedList = new List<KeyValuePairStringFloat>();
    public List<KeyValuePairBoolBool> boolSavedList = new List<KeyValuePairBoolBool>();

    // Temporary dictionaries for runtime access (not serialized)
    [System.NonSerialized]
    private Dictionary<string, SerializeVector3> _characterPosDict;
    [System.NonSerialized]
    private Dictionary<string, float> _floatSavedData;
    [System.NonSerialized]
    private Dictionary<bool, bool> _boolSavedData;

    // Property accessors, automatically sync lists and dictionaries
    public Dictionary<string, SerializeVector3> characterPosDict
    {
        get
        {
            if (_characterPosDict == null)
            {
                _characterPosDict = new Dictionary<string, SerializeVector3>();
                foreach (var item in characterPosList)
                {
                    _characterPosDict[item.key] = item.value;
                }
            }
            return _characterPosDict;
        }
    }

    public Dictionary<string, float> floatSavedData
    {
        get
        {
            if (_floatSavedData == null)
            {
                _floatSavedData = new Dictionary<string, float>();
                foreach (var item in floatSavedList)
                {
                    _floatSavedData[item.key] = item.value;
                }
            }
            return _floatSavedData;
        }
    }

    public Dictionary<bool, bool> boolSavedData
    {
        get
        {
            if (_boolSavedData == null)
            {
                _boolSavedData = new Dictionary<bool, bool>();
                foreach (var item in boolSavedList)
                {
                    _boolSavedData[item.key] = item.value;
                }
            }
            return _boolSavedData;
        }
    }

    // Initialize dictionaries from lists (called after loading)
    public void InitializeDictionariesFromLists()
    {
        _characterPosDict = new Dictionary<string, SerializeVector3>();
        foreach (var item in characterPosList)
        {
            _characterPosDict[item.key] = item.value;
        }

        _floatSavedData = new Dictionary<string, float>();
        foreach (var item in floatSavedList)
        {
            _floatSavedData[item.key] = item.value;
        }

        _boolSavedData = new Dictionary<bool, bool>();
        foreach (var item in boolSavedList)
        {
            _boolSavedData[item.key] = item.value;
        }
    }

    // Sync dictionaries to lists (called before saving)
    public void SyncDictionariesToLists()
    {
        characterPosList.Clear();
        foreach (var kvp in _characterPosDict ?? new Dictionary<string, SerializeVector3>())
        {
            characterPosList.Add(new KeyValuePairStringVector3 { key = kvp.Key, value = kvp.Value });
        }

        floatSavedList.Clear();
        foreach (var kvp in _floatSavedData ?? new Dictionary<string, float>())
        {
            floatSavedList.Add(new KeyValuePairStringFloat { key = kvp.Key, value = kvp.Value });
        }

        boolSavedList.Clear();
        foreach (var kvp in _boolSavedData ?? new Dictionary<bool, bool>())
        {
            boolSavedList.Add(new KeyValuePairBoolBool { key = kvp.Key, value = kvp.Value });
        }
    }

    public void SaveGameScene(GameSceneSO savedScene)
    {
        sceneToSave = JsonUtility.ToJson(savedScene);
        Debug.Log(sceneToSave);
    }

    public GameSceneSO GetSavedScene()
    {
        var newScene = ScriptableObject.CreateInstance<GameSceneSO>();
        JsonUtility.FromJsonOverwrite(sceneToSave, newScene);

        return newScene;
    }
}

// Serializable key-value pair class
[System.Serializable]
public class KeyValuePairStringVector3
{
    public string key;
    public SerializeVector3 value;
}

[System.Serializable]
public class KeyValuePairStringFloat
{
    public string key;
    public float value;
}

[System.Serializable]
public class KeyValuePairBoolBool
{
    public bool key;
    public bool value;
}

[System.Serializable]
public class SerializeVector3
{
    public float x, y, z;

    public SerializeVector3(Vector3 pos)
    {
        this.x = pos.x;
        this.y = pos.y;
        this.z = pos.z;
    }

    public SerializeVector3()
    {
        x = 0;
        y = 0;
        z = 0;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
