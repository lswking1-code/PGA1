using UnityEngine;
using UnityEngine.AI;

public class Chase : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform target;
    public GameObject[] wheels;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        agent.SetDestination(target.position);
        RotateWheels();
    }
    private void RotateWheels()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].transform.Rotate(-10, 0, 0);
        }
    }
}
