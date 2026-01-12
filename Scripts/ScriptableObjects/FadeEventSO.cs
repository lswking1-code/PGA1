using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "FadeEventSO", menuName = "Scriptable Objects/FadeEventSO")]
public class FadeEventSO : ScriptableObject
{
    [System.NonSerialized]
    private UnityAction<Color, float, bool> onEventRaised;
    
    // Property for backward compatibility
    public UnityAction<Color, float, bool> OnEventRaised
    {
        get { return onEventRaised; }
        set { onEventRaised = value; }
    }
    
    /// <summary>
    /// Fade in to black
    /// </summary>
    /// <param name="duration"></param>
    public void FadeIn(float duration)
    {
        RaiseEvent(Color.black, duration, true);
    }
    
    /// <summary>
    /// Fade out to transparent
    /// </summary>
    /// <param name="duration"></param>
    public void FadeOut(float duration)
    {
        RaiseEvent(Color.clear, duration, false);
    }

    public void RaiseEvent(Color target, float duration, bool fadeIn)
    {
        onEventRaised?.Invoke(target, duration, fadeIn);
    }
    
    // Add listener method
    public void AddListener(UnityAction<Color, float, bool> listener)
    {
        onEventRaised += listener;
    }
    
    // Remove listener method
    public void RemoveListener(UnityAction<Color, float, bool> listener)
    {
        onEventRaised -= listener;
    }
}
