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
    [Tooltip("Curve controlling the Collider expansion process. X-axis (0-1) represents time progress, Y-axis (0-1) represents interpolation value")]
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
        
        // Get SphereCollider from instantiated Scanner
        SphereCollider sphereCollider = Scanner.GetComponent<SphereCollider>();
        
        if (sphereCollider != null)
        {
            float initialRadius = sphereCollider.radius;
            
            // Start Collider expansion coroutine
            if (expandCoroutine != null)
            {
                StopCoroutine(expandCoroutine);
            }
            expandCoroutine = StartCoroutine(ExpandCollider(sphereCollider, initialRadius, Scanner));
        }
    }
    
    /// <summary>
    /// Coroutine: Gradually expand SphereCollider radius
    /// </summary>
    System.Collections.IEnumerator ExpandCollider(SphereCollider sphereCollider, float initialRadius, GameObject scannerInstance)
    {
        if (sphereCollider == null) yield break;
        
        float maxRadius = size * sizeProportion; 
        float elapsedTime = 0f;
        
        // Gradually expand from initial radius to max radius
        while (elapsedTime < duration && scannerInstance != null)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;
            // Evaluate interpolation value using curve
            float curveValue = expansionCurve.Evaluate(normalizedTime);
            float currentRadius = Mathf.Lerp(initialRadius, maxRadius, curveValue);
            
            // Check if instance still exists
            if (sphereCollider != null)
            {
                sphereCollider.radius = currentRadius;
            }
            
            yield return null;
        }
        
        // Ensure final max radius is reached (if instance still exists)
        if (scannerInstance != null && sphereCollider != null)
        {
            sphereCollider.radius = maxRadius;
        }
    }
   

    /// <summary>
    /// New Input System callback: called when "Scan" action bound in InputAction is triggered
    /// Requires using corresponding actions on PlayerInput and attaching this component to the same GameObject.
    /// </summary>
    /// <param name="value">Input value (key/button)</param>
    public void OnScan(InputValue value)
    {
        // Trigger scan on press moment (avoid repeated triggers when held)
        if (value.isPressed)
        {
            SpawnScan();
        }
    }
}