using UnityEngine;
using UnityEngine.Events;

public class GasCollected : MonoBehaviour
{
    [Header("Effect")]
    public GameObject ScannerPrefab;
    [Range(0, 100)]
    public float duration = 10;
    [Range(0, 1000)]
    public float size = 500;
    [Header("Attributes")]
    public float amount = 30;
    [Header("EventRaise")]
    public ResourceEventSO ResourceEvent;
    [Header("Animation")]
    private Animator animator;
    
    private bool isCollected = false; // Flag to prevent duplicate collection

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    /// <summary>
    /// Called when another Collider enters the trigger
    /// </summary>
    /// <param name="other">Collider that entered the trigger</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if entering object is Player
        if (other.CompareTag("Player"))
        {
            // Broadcast event
            ResourceEvent.RaiseEvent(amount);
            ResourceCollect();
        }
        else if(other.CompareTag("Scanner"))
        {
            animator.SetBool("Scan", true);
            SpawnScan();
        }
    }
    private void SpawnScan()
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
    public void ResourceCollect()
    {
        if (isCollected) return; // If already collected, return directly
        isCollected = true; // Mark as collected
        Destroy(gameObject);
    }
    public void ScanFinish()
    {
        animator.SetBool("Scan", false);
    }
}
