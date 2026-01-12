using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    [Header("UI")]
    public Slider timerSlider;
    public float sliderTimer;
    public float MaxTimer = 360;
    public bool stopTimer = false;
    public TMP_Text timerText;
    public GameSceneSO finalScene;
    [Header("EventRaise")]
    public VoidEventSO GameClearEvent;
    public VoidEventSO TimeoutEvent;
    [Header("EventListeners")]
    public SceneLoadEventSO unloadedSceneEvent;
    private Coroutine timerCoroutine;
    
    private bool isFinalScene = false;
    
    private void OnEnable()
    {
        unloadedSceneEvent.LoadRequestEvent += OnSceneLoadEvent;
    }
    
    private void OnDisable()
    {
        unloadedSceneEvent.LoadRequestEvent -= OnSceneLoadEvent;
    }
    
    private void OnSceneLoadEvent(GameSceneSO sceneToLoad, Vector3 posToGo, bool fadeScreen)
    {
        CheckIfFinalScene(sceneToLoad);
    }
    
    private void CheckIfFinalScene(GameSceneSO sceneToCheck)
    {
        if (finalScene == null || sceneToCheck == null)
        {
            isFinalScene = false;
            return;
        }
        
        // Directly compare GameSceneSO object or name
        if (sceneToCheck == finalScene || sceneToCheck.name == finalScene.name)
        {
            isFinalScene = true;
        }
        else
        {
            isFinalScene = false;
        }
    }
    

    public void StartTimer()
    {
        if(timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        stopTimer = false;
        timerCoroutine = StartCoroutine(StartTheTimerTicker());
        //DO NOT ADD LOGIC HERE
    }
    
    IEnumerator StartTheTimerTicker()
    {
        while(stopTimer == false)
        {
            sliderTimer -= Time.deltaTime;
            yield return new WaitForSeconds(0.001f);

            if(sliderTimer <= 0)
            {
                stopTimer = true;
            }
            if(!stopTimer)
            {
                timerSlider.value = sliderTimer;
                UpdateTimerText();
            }
        }
        //ADD YOUR GAME LOGIC HERE
        if(stopTimer)
        {   
            if(isFinalScene)
            {
                GameClearEvent.RaiseEvent();
            }
            else
            {
                TimeoutEvent.RaiseEvent();
            }
            
        }
        //EG RESPAWN CHARACTER LOGIC

    }
    public void StopTimer()
    {
        stopTimer = true;
    }
    
    private void UpdateTimerText()
    {
        if(timerText != null)
        {
            int minutes = Mathf.FloorToInt(sliderTimer / 60);
            int seconds = Mathf.FloorToInt(sliderTimer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    public void ResetTimer()
    {
        sliderTimer = MaxTimer;
        timerSlider.maxValue = MaxTimer;
        timerSlider.value = MaxTimer;
        UpdateTimerText();
        StartTimer();
    }
   
}
