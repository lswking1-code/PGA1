using UnityEngine;
using UnityEngine.InputSystem;

public class Scanner : MonoBehaviour
{
    public GameObject ScannerPrefab;
    [Range(0, 100)]
    public float duration = 10;
    [Range(0, 1000)]
    public float size = 500;
 
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