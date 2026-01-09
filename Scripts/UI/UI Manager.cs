using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
  public PlayerStatBar playerStatBar;
  [Header("EventListeners")]
  public SceneLoadEventSO unloadedSceneEvent;
  public VoidEventSO loadDataEvent;
  public VoidEventSO backToMenuEvent;
  public CharacterEventSO HealthEvent;


  [Header("UI")]
  public GameObject statBarUI;
  private void OnEnable()
  {
    HealthEvent.OnEventRaised += OnHealthEvent;
    unloadedSceneEvent.LoadRequestEvent += OnUnLoadedSceneEvent;
    loadDataEvent.OnEventRaised += OnloadDataEvent;
    backToMenuEvent.OnEventRaised += OnloadDataEvent;
  }
  private void OnDisable()
  {
    HealthEvent.OnEventRaised -= OnHealthEvent;
  }
  private void OnHealthEvent(Character character)
  {
     var percentage = character.HP / character.MaxHP;
     playerStatBar.OnHPChange(percentage);

     playerStatBar.currentCharacter = character;
  }
  private void OnUnLoadedSceneEvent(GameSceneSO sceneToLoad, Vector3 posToGo, bool fadeScreen)
  {
    var isMenu = sceneToLoad.sceneType == SceneType.Menu;
    statBarUI.SetActive(!isMenu);
  }
  private void OnloadDataEvent()
  {
    //TODO:通关/死亡UI关闭
  }
}
