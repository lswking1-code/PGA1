using UnityEngine;
using UnityEngine.Events;

public class GasCollected : MonoBehaviour
{
    [Header("事件设置")]
    [Tooltip("当Player进入触发器时触发此事件")]
    public UnityEvent OnPlayerEnter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 当其他Collider进入触发器时调用
    /// </summary>
    /// <param name="other">进入触发器的Collider</param>
    void OnTriggerEnter(Collider other)
    {
        // 检查进入的物体是否为Player
        if (other.CompareTag("Player"))
        {
            // 广播事件
            OnPlayerEnter?.Invoke();
        }
    }
}
