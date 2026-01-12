using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class Audio : MonoBehaviour
{
    [Header("Engine Clip")]
    public AudioClip EngineClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float MinimumVolume = 0.05f;
    [Range(0f, 1f)]
    public float MaximumVolume = 1f;

    [Header("Pitch")]
    public float MinimumPitch = 0.8f;
    public float MaximumPitch = 2.0f;

    [Header("Mapping")]
    [Tooltip("当速度达到此值时视为最大音量；低于此值按比例减小音量")]
    public float MaxSpeedForSound = 50f;
    [Tooltip("低于此速度视为空转（idle），用于指定空转时的音量计算阈值")]
    public float IdleThreshold = 0.5f;

    [Header("Background Music (played on scene loaded)")]
    public AudioClip BackgroundMusic;
    [Range(0f, 1f)] public float MusicVolume = 0.6f;
    public bool LoopBackgroundMusic = true;
    public bool PlayMusicOnSceneLoaded = true;

    private AudioSource audioSource;   // engine source
    private AudioSource musicSource;   // background music source
    private Drive drive;
    private Rigidbody rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 50f;

        if (EngineClip != null)
        {
            audioSource.clip = EngineClip;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"{nameof(Audio)} on '{gameObject.name}' has no EngineClip assigned. Audio disabled.");
            enabled = false;
            return;
        }

        // 创建并配置用于播放背景音乐的 AudioSource（2D 音效）
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = LoopBackgroundMusic;
        musicSource.spatialBlend = 0f; // 2D 音乐
        musicSource.volume = Mathf.Clamp01(MusicVolume);

        drive = GetComponent<Drive>() ?? GetComponentInParent<Drive>();
        rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
    }

    private void OnEnable()
    {
        // 订阅场景加载事件以在加载场景后播放背景音乐
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // SceneManager 回调 — 场景加载完成时触发
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!PlayMusicOnSceneLoaded) return;
        PlayBackgroundMusic();
    }

    // 主循环：根据车辆速度调整发动机音量与音调
    void Update()
    {
        float forwardSpeed = 0f;
        if (drive != null)
        {
            forwardSpeed = drive.GetCurrentSpeed();
        }
        else if (rb != null)
        {
            // 若项目环境使用不同 API，请替换 linearVelocity 为 velocity
            forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        }
        else
        {
            // 无法获取速度，使用 0 作为速度值以确保音频逻辑有合理输入
            SetAudio(Mathf.Abs(0f));
            return;
        }

        SetAudio(Mathf.Abs(forwardSpeed));
    }

    private void SetAudio(float speed)
    {
        // t : 0 .. 1，表示 speed 在 [IdleThreshold, MaxSpeedForSound] 区间内的归一化位置
        float t = Mathf.InverseLerp(IdleThreshold, MaxSpeedForSound, speed);

        // 计算目标音量：
        // 如果速度低于 IdleThreshold，则在 0 -> MinimumVolume 之间按比例提升；
        // 否则在 MinimumVolume -> MaximumVolume 之间根据 t 插值
        float targetVolume;
        if (speed < IdleThreshold)
            targetVolume = Mathf.Lerp(0f, MinimumVolume, speed / Mathf.Max(IdleThreshold, 0.0001f));
        else
            targetVolume = Mathf.Lerp(MinimumVolume, MaximumVolume, t);

        // 根据 t 计算目标音调（pitch）
        float targetPitch = Mathf.Lerp(MinimumPitch, MaximumPitch, t);

        // 平滑过渡到目标音量和音调
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, Time.deltaTime * 1.5f);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, Time.deltaTime * 1.5f);
    }

    // 播放背景音乐（可被外部调用）
    public void PlayBackgroundMusic()
    {
        if (BackgroundMusic == null)
        {
            Debug.LogWarning($"{nameof(Audio)}: no BackgroundMusic assigned on '{gameObject.name}'.");
        }
    }
}
