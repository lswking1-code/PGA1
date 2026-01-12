using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 基于项目的 `Wheel` 和 `InputProcessor` 实现车辆粒子效果：
/// - 每轮根据 WheelCollider 的 slip 控制轮胎粒子（实例化在 wheel.transform 下）
/// - 根据 InputProcessor.inputs.throttleInput 控制排气粒子发射速率
/// 将此脚本挂在车体根节点（与有 Rigidbody、Drive、InputProcessor 的对象同级或同一对象）
/// </summary>
[AddComponentMenu("PGA2/Player/ParticlesController")]
public class ParticlesController : MonoBehaviour
{
    [Header("Wheel Particles")]
    public ParticleSystem wheelParticlePrefab; // 若空则不实例化轮粒子
    [Tooltip("轮滑移阈值（侧向或前向）超过该值时触发粒子")]
    public float wheelSlipThreshold = 0.25f;

    
    [Tooltip("排气发射率范围（每秒）")]
    public float exhaustRateMin = 1f;
    public float exhaustRateMax = 20f;

    // runtime
    private InputProcessor inputProcessor;
    private Wheel[] wheels;
    private List<ParticleSystem> createdWheelParticles = new List<ParticleSystem>();
    private ParticleSystem.EmissionModule[] wheelEmissions;
    private ParticleSystem.EmissionModule[] exhaustEmissions;
    public WheelHit hit;

    private void Awake()
    {
        // input processor
        inputProcessor = GetComponent<InputProcessor>() ?? GetComponentInParent<InputProcessor>();

        // wheels
        wheels = GetComponentsInChildren<Wheel>();

        // instantiate wheel particle prefab per wheel if provided
        if (wheelParticlePrefab != null && wheels != null && wheels.Length > 0)
        {
            createdWheelParticles = new List<ParticleSystem>(wheels.Length);
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] == null) { createdWheelParticles.Add(null); continue; }
                var p = Instantiate(wheelParticlePrefab, wheels[i].transform.position, wheels[i].transform.rotation, wheels[i].transform);
                createdWheelParticles.Add(p);
            }

            wheelEmissions = new ParticleSystem.EmissionModule[createdWheelParticles.Count];
            for (int i = 0; i < createdWheelParticles.Count; i++)
            {
                wheelEmissions[i] = createdWheelParticles[i] != null ? createdWheelParticles[i].emission : default;
                if (createdWheelParticles[i] != null)
                    wheelEmissions[i].enabled = false;
            }
        }

        
    }

    private void Update()
    {
        UpdateWheelParticles(hit);
        
    }

    private void UpdateWheelParticles(WheelHit hit)
    {
        if (wheelParticlePrefab == null || createdWheelParticles == null || wheels == null) return;

        int len = Mathf.Min(wheels.Length, createdWheelParticles.Count);
        for (int i = 0; i < len; i++)
        {
            var wheel = wheels[i];
            var ps = createdWheelParticles[i];
            if (wheel == null || ps == null) continue;

            var emission = ps.emission;

           
            bool hasHit = wheel.wheelCollider != null && wheel.wheelCollider.GetGroundHit(out hit);

            bool emit = false;
            if (hasHit)
            {
                if (Mathf.Abs(hit.sidewaysSlip) >= wheelSlipThreshold || Mathf.Abs(hit.forwardSlip) >= wheelSlipThreshold)
                    emit = true;
            }

            emission.enabled = emit;
        }
    }

   
}