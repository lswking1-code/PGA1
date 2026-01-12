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
    [Tooltip("Speed at which maximum volume is reached; volume decreases proportionally below this value")]
    public float MaxSpeedForSound = 50f;
    [Tooltip("Speed below which is considered idle, used to specify idle volume calculation threshold")]
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
        // 检查场景中是否有 AudioListener，如果没有则尝试添加
        EnsureAudioListenerExists();

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        // 引擎音效使用2D音频，确保玩家始终能听到（不受距离影响）
        // 如果需要3D效果，可以改为 0.5f（混合）或 1f（完全3D）
        audioSource.spatialBlend = 0f; // 2D音频，跟随玩家
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 50f;
        audioSource.outputAudioMixerGroup = null; // 确保没有被静音
        audioSource.mute = false; // 确保未静音
        audioSource.bypassEffects = false;
        audioSource.bypassListenerEffects = false;
        audioSource.bypassReverbZones = false;

        if (EngineClip != null)
        {
            audioSource.clip = EngineClip;
            // 设置初始音量和音调，确保音频立即可以听到
            audioSource.volume = MinimumVolume;
            audioSource.pitch = MinimumPitch;
            
            // 确保音频源已启用
            if (!audioSource.enabled)
            {
                audioSource.enabled = true;
            }
            
            // 播放音频
            audioSource.Play();
            
            // 验证音频是否真的在播放
            if (!audioSource.isPlaying)
            {
                Debug.LogWarning($"{nameof(Audio)} on '{gameObject.name}': 音频源未能开始播放。检查 AudioSource 设置和 AudioListener。");
            }
            else
            {
                Debug.Log($"{nameof(Audio)} on '{gameObject.name}': 引擎音效已开始播放。");
            }
        }
        else
        {
            Debug.LogWarning($"{nameof(Audio)} on '{gameObject.name}' has no EngineClip assigned. Audio disabled.");
            enabled = false;
            return;
        }

        // Create and configure AudioSource for background music (2D audio)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = LoopBackgroundMusic;
        musicSource.spatialBlend = 0f; // 2D music
        musicSource.volume = Mathf.Clamp01(MusicVolume);

        drive = GetComponent<Drive>() ?? GetComponentInParent<Drive>();
        rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();

        // 如果场景已经加载完成，立即播放背景音乐（处理 OnSceneLoaded 可能不会触发的情况）
        if (PlayMusicOnSceneLoaded)
        {
            PlayBackgroundMusic();
        }
    }

    private void OnEnable()
    {
        // Subscribe to scene loaded event to play background music after scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // SceneManager callback - triggered when scene loading is complete
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!PlayMusicOnSceneLoaded) return;
        PlayBackgroundMusic();
    }

    // Main loop: adjust engine volume and pitch based on vehicle speed
    void Update()
    {
        // 确保音频源仍在播放，如果停止了则重新播放
        if (audioSource != null && EngineClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.LogWarning($"{nameof(Audio)} on '{gameObject.name}': 音频源意外停止，已重新播放。");
        }

        float forwardSpeed = 0f;
        if (drive != null)
        {
            forwardSpeed = drive.GetCurrentSpeed();
        }
        else if (rb != null)
        {
            // If project uses different API, replace linearVelocity with velocity
            forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        }
        else
        {
            // Cannot get speed, use 0 as speed value to ensure audio logic has reasonable input
            SetAudio(Mathf.Abs(0f));
            return;
        }

        SetAudio(Mathf.Abs(forwardSpeed));
    }

    private void SetAudio(float speed)
    {
        if (audioSource == null || !audioSource.enabled)
        {
            return;
        }

        // t : 0 .. 1, represents normalized position of speed in [IdleThreshold, MaxSpeedForSound] range
        float t = Mathf.InverseLerp(IdleThreshold, MaxSpeedForSound, speed);

        // Calculate target volume:
        // 即使速度为0，也保持最小音量（怠速音效）
        // If speed is below IdleThreshold, scale proportionally between MinimumVolume -> MinimumVolume;
        // Otherwise interpolate between MinimumVolume -> MaximumVolume based on t
        float targetVolume;
        if (speed < IdleThreshold)
        {
            // 确保即使速度为0时也有最小音量，而不是完全静音
            float idleRatio = Mathf.Clamp01(speed / Mathf.Max(IdleThreshold, 0.0001f));
            targetVolume = Mathf.Lerp(MinimumVolume * 0.3f, MinimumVolume, idleRatio);
        }
        else
            targetVolume = Mathf.Lerp(MinimumVolume, MaximumVolume, t);

        // Calculate target pitch based on t
        float targetPitch = Mathf.Lerp(MinimumPitch, MaximumPitch, t);

        // Smoothly transition to target volume and pitch
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, Time.deltaTime * 1.5f);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, Time.deltaTime * 1.5f);
    }

    // 确保场景中存在 AudioListener（Unity 音频系统需要它才能播放声音）
    private void EnsureAudioListenerExists()
    {
        AudioListener listener = FindObjectOfType<AudioListener>();
        
        if (listener == null)
        {
            // 尝试在主摄像机上添加 AudioListener
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // 如果没有主摄像机，尝试查找任何摄像机
                mainCamera = FindObjectOfType<Camera>();
            }

            if (mainCamera != null)
            {
                listener = mainCamera.gameObject.AddComponent<AudioListener>();
                Debug.LogWarning($"{nameof(Audio)}: 场景中没有 AudioListener，已在摄像机 '{mainCamera.name}' 上自动添加。");
            }
            else
            {
                Debug.LogError($"{nameof(Audio)}: 场景中没有 AudioListener 和 Camera！音频将无法播放。请确保场景中至少有一个带有 AudioListener 的摄像机。");
            }
        }
    }

    // Play background music (can be called externally)
    public void PlayBackgroundMusic()
    {
        if (BackgroundMusic == null)
        {
            Debug.LogWarning($"{nameof(Audio)}: no BackgroundMusic assigned on '{gameObject.name}'.");
            return;
        }

        if (musicSource == null)
        {
            Debug.LogWarning($"{nameof(Audio)}: musicSource is not initialized on '{gameObject.name}'.");
            return;
        }

        // 如果已经在播放相同的音乐，则不需要重复播放
        if (musicSource.isPlaying && musicSource.clip == BackgroundMusic)
        {
            return;
        }

        musicSource.clip = BackgroundMusic;
        musicSource.Play();
    }
}
