using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeCanva : MonoBehaviour
{
    [Header("EventListeners")]
    public FadeEventSO fadeEvent;

    [Header("UI")]
    public Image fadeImage;

    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        if (fadeEvent != null) fadeEvent.AddListener(OnFadeEvent);
    }
    private void OnDisable()
    {
        if (fadeEvent != null) fadeEvent.RemoveListener(OnFadeEvent);
    }

    private void OnFadeEvent(Color target, float duration, bool fadeIn)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeColor(target, duration));
    }
    private IEnumerator FadeColor(Color targetColor, float duration)
    {
        if (fadeImage == null)
            yield break;

        Color startColor = fadeImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        fadeImage.color = targetColor;
        fadeCoroutine = null;
    }
}
