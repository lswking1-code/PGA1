using UnityEngine;
using UnityEngine.Events;

public class GasCollected : MonoBehaviour
{
    [Header("Attributes")]
    public float Gas = 30;
    [Header("EventRaise")]
    public ResourceEventSO GasEvent;
    [Header("Animation")]
    private Animator animator;
    
    private bool isCollected = false; // 防止重复收集的标志

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
            GasEvent.RaiseEvent(Gas);
            GasCollect();
        }
        else if(other.CompareTag("Scanner"))
        {
            animator.SetBool("Scan", true);
        }
    }
    public void GasCollect()
    {
        if (isCollected) return; // 如果已经被收集，直接返回
        isCollected = true; // 标记为已收集
        Destroy(gameObject);
    }
    public void ScanFinish()
    {
        animator.SetBool("Scan", false);
    }
}
