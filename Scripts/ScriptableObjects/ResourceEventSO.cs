using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "ResourceEventSO", menuName = "Scriptable Objects/ResourceEventSO")]
public class ResourceEventSO : ScriptableObject
{
    public UnityAction<float> OnEventRaised;

    public void RaiseEvent(float amount)
    {
        OnEventRaised?.Invoke(amount);
    }
}
