using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveable
{
    DataDefination GetDataID();
    void RegisterSaveData() => DataManager.instance.RegisterSaveData(this);
    void UnregisterSaveData() => DataManager.instance.UnRegisterSaveData(this);
    
    void GetSaveData(Data data);
    void LoadSaveData(Data data);
}
