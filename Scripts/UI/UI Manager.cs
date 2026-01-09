using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
  public Timer timer;
  public PlayerStatBar playerStatBar;
  [Header("EventListeners")]
  public SceneLoadEventSO unloadedSceneEvent;
  public VoidEventSO loadDataEvent;
  public VoidEventSO backToMenuEvent;
  public CharacterEventSO HealthEvent;
  public VoidEventSO GameOverEvent;
  public VoidEventSO TimeoutEvent;


  [Header("UI")]
  public GameObject statBarUI;
  public GameObject timerUI;
  public GameObject gameOverUI;
  public GameObject timeoutUI;



  private void OnEnable()
  {
    HealthEvent.OnEventRaised += OnHealthEvent;
    unloadedSceneEvent.LoadRequestEvent += OnUnLoadedSceneEvent;
    loadDataEvent.OnEventRaised += OnloadDataEvent;
    backToMenuEvent.OnEventRaised += OnloadDataEvent;
    TimeoutEvent.OnEventRaised += OnTimeoutEvent;
    GameOverEvent.OnEventRaised += OnGameOverEvent;
  }
  private void OnDisable()
  {
    HealthEvent.OnEventRaised -= OnHealthEvent;
    unloadedSceneEvent.LoadRequestEvent -= OnUnLoadedSceneEvent;
    loadDataEvent.OnEventRaised -= OnloadDataEvent;
    backToMenuEvent.OnEventRaised -= OnloadDataEvent;
    TimeoutEvent.OnEventRaised -= OnTimeoutEvent;
    GameOverEvent.OnEventRaised -= OnGameOverEvent;
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
    timerUI.SetActive(!isMenu);
  }
  private void OnloadDataEvent()
  {
    timeoutUI.SetActive(false);
    gameOverUI.SetActive(false);
    timer.ResetTimer();
  }
  private void OnTimeoutEvent()
  {
    timeoutUI.SetActive(true);
  }
  private void OnGameOverEvent()
  {
    gameOverUI.SetActive(true);
  }
}
