using UnityEngine;

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
    [Tooltip("�����ٶȴﵽ��ֵʱ������/�����ﵽ���")]
    public float MaxSpeedForSound = 50f;
    [Tooltip("���ڴ��ٶ���Ϊ���٣������ή���ܵ�")]
    public float IdleThreshold = 0.5f;

    private AudioSource audioSource;
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

        drive = GetComponent<Drive>() ?? GetComponentInParent<Drive>();
        rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        
        float forwardSpeed = 0f;
        if (drive != null)
        {
            forwardSpeed = drive.GetCurrentSpeed();
        }
        else if (rb != null)
        {
            forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        }
        else
        {
            // �޷���ȡ�ٶȣ�������С�������������
            SetAudio(Mathf.Abs(0f));
            return;
        }

        SetAudio(Mathf.Abs(forwardSpeed));
    }

    private void SetAudio(float speed)
    {
        // t : 0 .. 1 ���ڲ�ֵ�����ǵ������ޣ�
        float t = Mathf.InverseLerp(IdleThreshold, MaxSpeedForSound, speed);

        // ��������������ƽ������С��ֵ������ IdleThreshold ���������������
        float targetVolume;
        if (speed < IdleThreshold)
            targetVolume = Mathf.Lerp(0f, MinimumVolume, speed / Mathf.Max(IdleThreshold, 0.0001f));
        else
            targetVolume = Mathf.Lerp(MinimumVolume, MaximumVolume, t);

        // ���������� t ����ӳ��
        float targetPitch = Mathf.Lerp(MinimumPitch, MaximumPitch, t);

        // ƽ������
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, Time.deltaTime * 1.5f);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, Time.deltaTime * 1.5f);
    }
}
