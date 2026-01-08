using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "GameSceneSO", menuName = "Scriptable Objects/GameSceneSO")]
public class GameSceneSO : ScriptableObject
{
    public SceneType sceneType;

    public AssetReference sceneReference;
}
