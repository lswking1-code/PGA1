using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SceneLoadEventSO", menuName = "Scriptable Objects/SceneLoadEventSO")]
public class SceneLoadEventSO : ScriptableObject
{
    public UnityAction<GameSceneSO, Vector3, bool> LoadRequestEvent;

    /// <summary>
    /// Broadcast load request
    /// </summary>
    /// <param name="locationToLoad">Scene to load</param>
    /// <param name="posToGo">Player's target position</param>
    /// <param name="fadeScreen">Whether to fade</param>
    public void RaiseLoadRequestEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        LoadRequestEvent?.Invoke(locationToLoad, posToGo, fadeScreen);
    }
}
