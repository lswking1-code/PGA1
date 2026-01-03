using UnityEngine;

public class Chasing : MonoBehaviour
{
    [Header("Collision")]
    [Range(0, 1000)] 
    public float pushForce = 100f; // 对Player施加的力的大小
    [Range(0, 100)] 
    public float collisionForce = 10f; // 对Chaser施加的力的大小
    [Range(0, 1000)] 
    public float recoilForce = 50f; // 撞击玩家后敌人受到的向后力的大小
    public float Damage = 10;
    public float SelfDamage = 20;
    
    [Header("Collision Cooldown")]
    [Range(0, 5)]
    public float collisionCooldown = 1f; // 碰撞冷却时间（秒）
    
    private Rigidbody rb; // Rigidbody 组件，用于物理移动
    private Character character;
    private float lastPlayerCollisionTime = -1f; // 上次与玩家碰撞的时间
    private float lastChaserCollisionTime = -1f; // 上次与其他Chaser碰撞的时间

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

    // 处理与其他 Chaser 的碰撞
    private void ChaserCollision(GameObject chaser)
    {
        // 检查碰撞冷却时间
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

    // 处理与Player的碰撞
    private void PlayerCollision(GameObject player)
    {
        // 检查碰撞冷却时间
        if (Time.time - lastPlayerCollisionTime < collisionCooldown)
            return;
        
        lastPlayerCollisionTime = Time.time;
        
        // 检查 character 是否存在
        if (character == null)
        {
            Debug.LogWarning("Chasing: Character component is null!");
            return;
        }
        
        // 计算力的方向（从玩家指向敌人）
        Vector3 forceDirection = (player.transform.position - transform.position).normalized;
        forceDirection.y = 0;
        
        // 对Player施加力
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.AddForce(forceDirection * pushForce, ForceMode.Impulse);
        }
        
        // 对敌人自身施加向后的力（与玩家受到的力方向相反）
        if (rb != null)
        {
            Vector3 recoilDirection = -forceDirection; // 向后方向
            rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);
        }
        
        // 对敌人造成伤害
        character.TakeDamage(SelfDamage);
    }
}
