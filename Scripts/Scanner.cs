using UnityEngine;
using UnityEngine.InputSystem;

public class Scanner : MonoBehaviour
{
    [Header("Effect")]
    public GameObject ScannerPrefab;
    [Range(0, 100)]
    public float duration = 10;
    [Range(0, 1000)]
    public float size = 500;
    [Header("Collicion")]
    [Range(0, 1)]
    public float sizeProportion = 0.5f;
    [Tooltip("控制Collider放大过程的曲线。X轴(0-1)表示时间进度，Y轴(0-1)表示插值值")]
    public AnimationCurve expansionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    private Coroutine expandCoroutine;
    
    void SpawnScan()
    {
        GameObject Scanner = Instantiate(ScannerPrefab, gameObject.transform.position, Quaternion.identity) as GameObject;
        ParticleSystem ScannerPS = Scanner.transform.GetChild(0).GetComponent<ParticleSystem>();
        if(ScannerPS != null)
        {
            var main = ScannerPS.main;
            main.startLifetime = duration;
            main.startSize = size;
        }
        Destroy(Scanner, duration+1);
        
        // 从实例化的Scanner上获取SphereCollider
        SphereCollider sphereCollider = Scanner.GetComponent<SphereCollider>();
        
        if (sphereCollider != null)
        {
            float initialRadius = sphereCollider.radius;
            
            // 启动Collider扩展协程
            if (expandCoroutine != null)
            {
                StopCoroutine(expandCoroutine);
            }
            expandCoroutine = StartCoroutine(ExpandCollider(sphereCollider, initialRadius, Scanner));
        }
    }
    
    /// <summary>
    /// 协程：逐渐放大SphereCollider的半径
    /// </summary>
    System.Collections.IEnumerator ExpandCollider(SphereCollider sphereCollider, float initialRadius, GameObject scannerInstance)
    {
        if (sphereCollider == null) yield break;
        
        float maxRadius = size * sizeProportion; 
        float elapsedTime = 0f;
        
        // 从初始半径逐渐放大到最大半径
        while (elapsedTime < duration && scannerInstance != null)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;
            // 使用曲线评估插值值
            float curveValue = expansionCurve.Evaluate(normalizedTime);
            float currentRadius = Mathf.Lerp(initialRadius, maxRadius, curveValue);
            
            // 检查实例是否还存在
            if (sphereCollider != null)
            {
                sphereCollider.radius = currentRadius;
            }
            
            yield return null;
        }
        
        // 确保最终达到最大半径（如果实例还存在）
        if (scannerInstance != null && sphereCollider != null)
        {
            sphereCollider.radius = maxRadius;
        }
    }
   

    /// <summary>
    /// 新 Input System 回调：在 InputAction 中绑定的 "Scan" 行为触发时被调用
    /// 需要在 PlayerInput 上使用对应的 actions，并将此组件挂在同一个 GameObject 上。
    /// </summary>
    /// <param name="value">输入值（按键/按钮）</param>
    public void OnScan(InputValue value)
    {
        // 按下瞬间触发扫描（避免按住时重复触发）
        if (value.isPressed)
        {
            SpawnScan();
        }
    }
}