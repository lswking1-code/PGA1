using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    public SceneLoadEventSO loadEventSO;
    public GameSceneSO[] sceneToGo;
    public Vector3 positionToGo;
    public VoidEventSO GameClearEvent;
    public GameSceneSO[] allGameScenes;

    public void GoNextLevel()
    {
        Debug.Log("GO TO NEXT LEVEL");
        
        int currentSceneIndex = FindCurrentSceneIndex();
        Debug.Log($"Current Scene Index: {currentSceneIndex}");
        
        if (currentSceneIndex == -1)
        {
            if (sceneToGo != null && sceneToGo.Length > 0 && sceneToGo[0] != null)
            {
                loadEventSO.RaiseLoadRequestEvent(sceneToGo[0], positionToGo, true);
            }
            return;
        }
        
        // 检查是否存在下一个顺位的场景
        int nextSceneIndex = currentSceneIndex + 1;
        if (nextSceneIndex < allGameScenes.Length && allGameScenes[nextSceneIndex] != null)
        {
            GameSceneSO nextScene = allGameScenes[nextSceneIndex];
            Debug.Log($"Found Next Scene: {nextScene.name} (Index: {nextSceneIndex})");
            
            // 设置 sceneToGo 并加载
            if (sceneToGo == null || sceneToGo.Length == 0)
            {
                sceneToGo = new GameSceneSO[1];
            }
            sceneToGo[0] = nextScene;
            loadEventSO.RaiseLoadRequestEvent(nextScene, positionToGo, true);
        }
        else
        {
            Debug.Log($"Scene Not Found (Now Index: {currentSceneIndex}, Array Length: {allGameScenes.Length})");
            // 广播游戏通关事件
            /*if (GameClearEvent != null)
            {
                GameClearEvent.RaiseEvent();
            }*/
        }
    }
    
    /// <summary>
    /// 查找当前场景在 allGameScenes 数组中的索引位置（忽略 Persistent 场景）
    /// </summary>
    private int FindCurrentSceneIndex()
    {
        if (allGameScenes == null || allGameScenes.Length == 0)
        {
            Debug.LogWarning("allGameScenes is not set or empty");
            return -1;
        }
        
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(sceneIndex);
            string sceneName = loadedScene.name;
            
            // ignore Persistent scene
            if (sceneName.Equals("Persistent", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            Debug.Log($"Checked Loaded Scene: {sceneName}");
            
            // find matching scene in allGameScenes array
            for (int i = 0; i < allGameScenes.Length; i++)
            {
                GameSceneSO scene = allGameScenes[i];
                if (scene == null)
                    continue;
                
                // check if scene name matches
                if (scene.name == sceneName || 
                    scene.name.Contains(sceneName) || 
                    sceneName.Contains(scene.name))
                {
                    Debug.Log($"Found Matching Scene: {scene.name} (Index: {i})");
                    return i;
                }
            }
        }
        
        return -1;
    }
}
