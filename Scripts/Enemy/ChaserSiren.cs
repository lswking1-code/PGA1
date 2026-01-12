using UnityEngine;

/// <summary>
/// Chaser 的警笛音效组件，通过 ChaserSirenManager 管理播放
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ChaserSiren : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("警笛音效片段")]
    public AudioClip sirenClip;
    
    [Tooltip("音量")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Header("Priority Settings")]
    [Tooltip("优先级（数值越小优先级越高，当超过最大数量时会替换低优先级的）")]
    [Range(0f, 100f)]
    public float priority = 50f;

    [Header("Auto Play Settings")]
    [Tooltip("是否在 Start 时自动请求播放")]
    public bool playOnStart = true;
    
    [Tooltip("是否在距离玩家一定范围内时自动播放")]
    public bool playWhenNearPlayer = true;
    
    [Tooltip("自动播放的距离阈值")]
    [Range(0f, 100f)]
    public float autoPlayDistance = 50f;

    private AudioSource audioSource;
    private bool isRequestingPlay = false;
    private bool isPlaying = false;
    private Transform playerTransform;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置 AudioSource
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D 音频，确保能听到
        audioSource.volume = volume;
        audioSource.clip = sirenClip;
    }

    private void Start()
    {
        // 注册到管理器
        if (ChaserSirenManager.Instance != null)
        {
            ChaserSirenManager.Instance.RegisterSiren(this);
        }

        // 查找玩家
        FindPlayer();

        // 如果设置了自动播放，请求播放
        if (playOnStart)
        {
            RequestPlay();
        }
    }

    private void Update()
    {
        // 如果设置了根据距离自动播放
        if (playWhenNearPlayer && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= autoPlayDistance)
            {
                if (!isRequestingPlay)
                {
                    RequestPlay();
                }
            }
            else
            {
                if (isRequestingPlay)
                {
                    Stop();
                }
            }
        }

        // 如果玩家丢失，尝试重新查找
        if (playerTransform == null)
        {
            FindPlayer();
        }
    }

    /// <summary>
    /// 查找玩家 Transform
    /// </summary>
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    /// <summary>
    /// 请求播放警笛音效
    /// </summary>
    public void RequestPlay()
    {
        if (isRequestingPlay)
        {
            return;
        }

        isRequestingPlay = true;

        if (ChaserSirenManager.Instance != null)
        {
            ChaserSirenManager.Instance.RequestPlaySiren(this);
        }
        else
        {
            Debug.LogWarning($"{nameof(ChaserSiren)} on '{gameObject.name}': ChaserSirenManager 实例不存在！");
        }
    }

    /// <summary>
    /// 停止播放警笛音效
    /// </summary>
    public void Stop()
    {
        if (!isRequestingPlay)
        {
            return;
        }

        isRequestingPlay = false;

        if (ChaserSirenManager.Instance != null)
        {
            ChaserSirenManager.Instance.StopSiren(this);
        }
    }

    /// <summary>
    /// 设置优先级（可以在运行时动态调整）
    /// </summary>
    public void SetPriority(float newPriority)
    {
        priority = newPriority;
        
        if (ChaserSirenManager.Instance != null)
        {
            ChaserSirenManager.Instance.UpdateSirenPriority(this);
        }
    }

    /// <summary>
    /// 检查是否正在请求播放
    /// </summary>
    public bool IsRequestingPlay()
    {
        return isRequestingPlay;
    }

    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }

    /// <summary>
    /// 当警笛播放请求被批准时调用（由 ChaserSirenManager 调用）
    /// </summary>
    public void OnSirenApproved()
    {
        if (audioSource != null && sirenClip != null && !audioSource.isPlaying)
        {
            audioSource.clip = sirenClip;
            audioSource.volume = volume;
            audioSource.Play();
            isPlaying = true;
        }
    }

    /// <summary>
    /// 当警笛播放请求被拒绝时调用（由 ChaserSirenManager 调用）
    /// </summary>
    public void OnSirenRejected()
    {
        // 可以在这里添加被拒绝时的逻辑（例如显示提示）
        // 当前不做任何处理，因为可能稍后会被批准
    }

    /// <summary>
    /// 当警笛停止播放时调用（由 ChaserSirenManager 调用）
    /// </summary>
    public void OnSirenStopped()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        isPlaying = false;
    }

    private void OnDestroy()
    {
        // 注销
        if (ChaserSirenManager.Instance != null)
        {
            ChaserSirenManager.Instance.UnregisterSiren(this);
        }
    }

    private void OnDisable()
    {
        // 禁用时停止播放
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        isPlaying = false;
    }
}
