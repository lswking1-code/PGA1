using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("UI")]
    public Slider timerSlider;
    public float sliderTimer;
    public float MaxTimer = 360;
    public bool stopTimer = false;
    public TMP_Text timerText;

    public VoidEventSO TimeoutEvent;
    public void Start()
    {
        timerSlider.maxValue = sliderTimer;
        timerSlider.value = sliderTimer;
        UpdateTimerText();
        StartTimer();
    }

    public void StartTimer()
    {
        StartCoroutine(StartTheTimerTicker());
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
            if(stopTimer == false)
            {
                timerSlider.value = sliderTimer;
                UpdateTimerText();
            }
        }
        //ADD YOUR GAME LOGIC HERE
        if(stopTimer == true)
        {   
            TimeoutEvent.RaiseEvent();
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
    }
   
}
