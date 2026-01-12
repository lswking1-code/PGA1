using System.Collections;
using UnityEngine;

[AddComponentMenu("PGA2/Camera/CameraShake")]
public class CameraShake : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera Transform (automatically finds Camera.main if not specified)")]
    public Transform cameraTransform;

    [Header("Shake Settings")]
    [Tooltip("Shake intensity (in meters)")]
    public float intensity = 0.3f;
    [Tooltip("Shake duration (in seconds)")]
    public float duration = 0.5f;
    [Tooltip("Shake frequency (updates per second, higher values are smoother)")]
    public float frequency = 60f;

    private Vector3 shakeOffset = Vector3.zero;
    private Vector3 lastCameraPosition;
    private Coroutine shakeRoutine;
    private bool isShaking = false;
    private Camera targetCamera;
    private bool isOnCameraObject = false; // Whether the script is on the camera object

    private void Awake()
    {
        FindCamera();
    }

    private void OnEnable()
    {
        // Ensure camera is re-found when enabled
        if (cameraTransform == null)
        {
            FindCamera();
        }
    }

    private void FindCamera()
    {
        if (cameraTransform == null)
        {
            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
                Debug.Log($"CameraShake: Automatically found camera - {cameraTransform.name}");
            }
            else
            {
                // If Camera.main doesn't exist, try to find any camera
                targetCamera = FindObjectOfType<Camera>();
                if (targetCamera != null)
                {
                    cameraTransform = targetCamera.transform;
                    Debug.Log($"CameraShake: Found camera in scene - {cameraTransform.name}");
                }
            }
        }
        else
        {
            targetCamera = cameraTransform.GetComponent<Camera>();
        }

        // Check if script is on the camera object
        isOnCameraObject = (targetCamera != null && targetCamera.transform == transform);

        if (cameraTransform == null)
        {
            Debug.LogError("CameraShake: Unable to find camera! Please ensure there is a Camera component in the scene, or manually specify cameraTransform in the Inspector.");
        }
    }

    // If script is on camera object, use OnPreRender (executes last before rendering)
    private void OnPreRender()
    {
        if (isOnCameraObject && isShaking && cameraTransform != null)
        {
            cameraTransform.position += shakeOffset;
        }
    }

    private void OnPostRender()
    {
        if (isOnCameraObject && isShaking && cameraTransform != null)
        {
            cameraTransform.position -= shakeOffset;
        }
    }

    // Use LateUpdate as fallback (when script is not on camera object)
    private void LateUpdate()
    {
        if (!isOnCameraObject && isShaking && cameraTransform != null)
        {
            // Save current camera position (Cinemachine may have already updated it)
            lastCameraPosition = cameraTransform.position;
            
            // Apply shake offset (in world coordinate space)
            cameraTransform.position = lastCameraPosition + shakeOffset;
        }
    }

    public void StartShake()
    {
        // If camera reference is lost, try to re-find it
        if (cameraTransform == null)
        {
            FindCamera();
            if (cameraTransform == null)
            {
                Debug.LogWarning("CameraShake: cameraTransform is null, cannot start shake!");
                return;
            }
        }

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        isShaking = true;
        shakeRoutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        Debug.Log($"Camera Shake Started - Camera: {cameraTransform.name}, Intensity: {intensity}, Duration: {duration}");
    }

    private IEnumerator ShakeCoroutine(float shakeIntensity, float shakeDuration)
    {
        float elapsed = 0f;
        float interval = 1f / Mathf.Max(1f, frequency);

        while (elapsed < shakeDuration)
        {
            // Generate shake using random offset, dampened by remaining time
            float damper = 1f - (elapsed / shakeDuration);
            
            // Generate random offset
            shakeOffset = Random.insideUnitSphere * shakeIntensity * damper;

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // Reset offset
        shakeOffset = Vector3.zero;
        isShaking = false;
        shakeRoutine = null;
        
        Debug.Log("Camera Shake Ended");
    }

    // Provide parameterized version to allow dynamic parameter adjustment
    public void StartShake(float customIntensity, float customDuration)
    {
        float oldIntensity = intensity;
        float oldDuration = duration;
        
        intensity = customIntensity;
        duration = customDuration;
        
        StartShake();
        
        // Restore original values (optional, depends on your needs)
        // intensity = oldIntensity;
        // duration = oldDuration;
    }
}
