using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理所有 Chaser 的警笛音效，确保同时播放的警笛音效数量不超过3个
/// </summary>
public class ChaserSirenManager : MonoBehaviour
{
    private static ChaserSirenManager instance;
    public static ChaserSirenManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 尝试查找现有实例
                instance = FindObjectOfType<ChaserSirenManager>();
                
                // 如果不存在，创建一个新的 GameObject 并附加管理器
                if (instance == null)
                {
                    GameObject managerObject = new GameObject("ChaserSirenManager");
                    instance = managerObject.AddComponent<ChaserSirenManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }
            return instance;
        }
    }

    [Header("Settings")]
    [Tooltip("同时播放的警笛音效最大数量")]
    [Range(1, 10)]
    public int maxActiveSirens = 3;

    // 当前正在播放警笛的 ChaserSiren 列表（按优先级排序）
    private List<ChaserSiren> activeSirens = new List<ChaserSiren>();
    // 所有注册的 ChaserSiren（包括未播放的）
    private HashSet<ChaserSiren> registeredSirens = new HashSet<ChaserSiren>();

    private void Awake()
    {
        // 确保只有一个实例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning("ChaserSirenManager: 检测到多个实例，销毁重复的实例。");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 注册一个 ChaserSiren（当 Chaser 被创建时调用）
    /// </summary>
    public void RegisterSiren(ChaserSiren siren)
    {
        if (siren != null && !registeredSirens.Contains(siren))
        {
            registeredSirens.Add(siren);
        }
    }

    /// <summary>
    /// 注销一个 ChaserSiren（当 Chaser 被销毁时调用）
    /// </summary>
    public void UnregisterSiren(ChaserSiren siren)
    {
        if (siren != null)
        {
            registeredSirens.Remove(siren);
            StopSiren(siren);
        }
    }

    /// <summary>
    /// 请求播放警笛音效
    /// </summary>
    /// <param name="siren">请求播放的 ChaserSiren</param>
    /// <returns>是否成功开始播放</returns>
    public bool RequestPlaySiren(ChaserSiren siren)
    {
        if (siren == null)
        {
            return false;
        }

        // 如果已经在播放列表中，直接返回成功
        if (activeSirens.Contains(siren))
        {
            return true;
        }

        // 如果未达到最大数量，直接添加
        if (activeSirens.Count < maxActiveSirens)
        {
            activeSirens.Add(siren);
            siren.OnSirenApproved();
            return true;
        }

        // 如果已达到最大数量，检查是否有更低优先级的可以替换
        // 找到优先级最低的（优先级值越小，优先级越高）
        ChaserSiren lowestPrioritySiren = null;
        float lowestPriority = float.MaxValue;

        foreach (var activeSiren in activeSirens)
        {
            if (activeSiren != null && activeSiren.priority < lowestPriority)
            {
                lowestPriority = activeSiren.priority;
                lowestPrioritySiren = activeSiren;
            }
        }

        // 如果新请求的优先级更高，替换最低优先级的
        if (lowestPrioritySiren != null && siren.priority < lowestPriority)
        {
            lowestPrioritySiren.OnSirenRejected();
            activeSirens.Remove(lowestPrioritySiren);
            activeSirens.Add(siren);
            siren.OnSirenApproved();
            return true;
        }

        // 无法播放（已达到最大数量且优先级不够高）
        siren.OnSirenRejected();
        return false;
    }

    /// <summary>
    /// 停止播放警笛音效
    /// </summary>
    public void StopSiren(ChaserSiren siren)
    {
        if (siren != null && activeSirens.Contains(siren))
        {
            activeSirens.Remove(siren);
            siren.OnSirenStopped();
        }
    }

    /// <summary>
    /// 更新播放列表（当优先级改变时调用）
    /// </summary>
    public void UpdateSirenPriority(ChaserSiren siren)
    {
        if (siren == null)
        {
            return;
        }

        // 如果不在播放列表中，尝试请求播放
        if (!activeSirens.Contains(siren))
        {
            RequestPlaySiren(siren);
        }
        else
        {
            // 如果在播放列表中，检查是否应该被替换
            // 重新评估所有请求播放的 siren
            RefreshActiveSirens();
        }
    }

    /// <summary>
    /// 刷新活动警笛列表，确保只有优先级最高的在播放
    /// </summary>
    private void RefreshActiveSirens()
    {
        // 收集所有请求播放的 siren
        List<ChaserSiren> requestingSirens = new List<ChaserSiren>();
        foreach (var siren in registeredSirens)
        {
            if (siren != null && siren.IsRequestingPlay())
            {
                requestingSirens.Add(siren);
            }
        }

        // 按优先级排序（优先级值越小，优先级越高）
        requestingSirens.Sort((a, b) => a.priority.CompareTo(b.priority));

        // 停止所有当前播放的
        foreach (var activeSiren in activeSirens)
        {
            if (activeSiren != null)
            {
                activeSiren.OnSirenStopped();
            }
        }
        activeSirens.Clear();

        // 选择优先级最高的最多 maxActiveSirens 个
        int count = Mathf.Min(requestingSirens.Count, maxActiveSirens);
        for (int i = 0; i < count; i++)
        {
            activeSirens.Add(requestingSirens[i]);
            requestingSirens[i].OnSirenApproved();
        }
    }

    /// <summary>
    /// 获取当前正在播放的警笛数量
    /// </summary>
    public int GetActiveSirenCount()
    {
        return activeSirens.Count;
    }

    /// <summary>
    /// 清理无效引用
    /// </summary>
    private void Update()
    {
        // 定期清理无效引用
        activeSirens.RemoveAll(siren => siren == null);
        registeredSirens.RemoveWhere(siren => siren == null);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
