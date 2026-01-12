using UnityEngine;

public class Chasing : MonoBehaviour
{
    [Header("Collision")]
    [Range(0, 1000)] 
    public float pushForce = 100f; // Force magnitude applied to Player
    [Range(0, 100)] 
    public float collisionForce = 10f; // Force magnitude applied to Chaser
    [Range(0, 1000)] 
    public float recoilForce = 50f; // Backward force magnitude enemy receives after hitting player
    public float Damage = 10;
    public float SelfDamage = 20;
    
    [Header("Collision Cooldown")]
    [Range(0, 5)]
    public float collisionCooldown = 1f; // Collision cooldown time (in seconds)
    
    private Rigidbody rb; // Rigidbody component for physics movement
    private Character character;
    private float lastPlayerCollisionTime = -1f; // Time of last collision with player
    private float lastChaserCollisionTime = -1f; // Time of last collision with other Chaser

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        character = GetComponent<Character>();
    }    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PlayerCollision(other.gameObject);
        else if (other.CompareTag("Chaser"))
            ChaserCollision(other.gameObject);
    }

    // Handle collision with other Chaser
    private void ChaserCollision(GameObject chaser)
    {
        // Check collision cooldown time
        if (Time.time - lastChaserCollisionTime < collisionCooldown)
            return;
        
        lastChaserCollisionTime = Time.time;
        
        Rigidbody chaserRb = chaser.GetComponent<Rigidbody>();
        if (chaserRb != null)
        {
            Vector3 force = (chaser.transform.position - transform.position).normalized;
            force.y = 0;
            chaserRb.AddForce(force * collisionForce, ForceMode.Impulse);
        }
    }

    // Handle collision with Player
    private void PlayerCollision(GameObject player)
    {
        // Check collision cooldown time
        if (Time.time - lastPlayerCollisionTime < collisionCooldown)
            return;
        
        lastPlayerCollisionTime = Time.time;
        
        // Check if character exists
        if (character == null)
        {
            Debug.LogWarning("Chasing: Character component is null!");
            return;
        }
        
        // Calculate force direction (from player to enemy)
        Vector3 forceDirection = (player.transform.position - transform.position).normalized;
        forceDirection.y = 0;
        
        // Apply force to Player
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.AddForce(forceDirection * pushForce, ForceMode.Impulse);
        }
        
        // Apply backward force to enemy itself (opposite direction to force on player)
        if (rb != null)
        {
            Vector3 recoilDirection = -forceDirection; // Backward direction
            rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);
        }
        
        // Deal damage to enemy
        character.TakeDamage(SelfDamage);
    }
}
