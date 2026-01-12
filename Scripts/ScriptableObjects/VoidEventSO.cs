using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "VoidEventSO", menuName = "Scriptable Objects/VoidEventSO")]
public class VoidEventSO : ScriptableObject
{
    [System.NonSerialized]
    private UnityAction onEventRaised;
    
    // Property for backward compatibility
    public UnityAction OnEventRaised
    {
        get { return onEventRaised; }
        set { onEventRaised = value; }
    }

    public void RaiseEvent()
    {
        Debug.Log($"VoidEventSO: RaiseEvent called on {name}. Subscribers: {(onEventRaised != null ? onEventRaised.GetInvocationList().Length : 0)}");
        onEventRaised?.Invoke();
    }
    
    // Add listener method (solves WebGL platform ScriptableObject serialization issues)
    public void AddListener(UnityAction listener)
    {
        onEventRaised += listener;
    }
    
    // Remove listener method
    public void RemoveListener(UnityAction listener)
    {
        onEventRaised -= listener;
    }
}
