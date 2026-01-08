using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class DriveController : MonoBehaviour
{
    [Header("EventListeners")]
    public SceneLoadEventSO sceneloadEvent;
    public VoidEventSO afterSceneLoadedEvent;
    public VoidEventSO LoadDataEvent;
    public VoidEventSO backToMenuEvent;
    public ResourceEventSO GasEvent;
    public ResourceEventSO HPEvent;

    [Header("Attributes")]
    private Character character;
    
    [Header("Input")]
    public float gasInput;
    public float brakeInput;
    public Vector2 steeringInput;
    
    [Header("Physics")]
    private Drive drive;
    
    

    private Rigidbody rb;
    private float horizontalInput;
    private float forwardInput;
    private void OnEnable()
    {
        sceneloadEvent.LoadRequestEvent += OnloadEvent;
        afterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadedEvent;
        LoadDataEvent.OnEventRaised += OnloadDataEvent;
        backToMenuEvent.OnEventRaised += OnloadDataEvent;
        GasEvent.OnEventRaised += OnGasEvent;
        HPEvent.OnEventRaised += OnHPEvent;
    }
    private void OnDisable()
    {
        sceneloadEvent.LoadRequestEvent -= OnloadEvent;
        afterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
        LoadDataEvent.OnEventRaised -= OnloadDataEvent;
        backToMenuEvent.OnEventRaised -= OnloadDataEvent;
        GasEvent.OnEventRaised -= OnGasEvent;
        HPEvent.OnEventRaised -= OnHPEvent;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        character = GetComponent<Character>();
        drive = GetComponent<Drive>();
    }


    void FixedUpdate()
    {
        if(character.Gas > 0)
        {
            drive.Driving(gasInput, brakeInput, steeringInput);
        }
    }
    
    void LateUpdate()
    {
        if (rb == null) return;
        //ConstrainToNavMesh();
    }

    public void OnAccelerate(InputValue button)
    {
        if(button.isPressed)
        {
            gasInput = 1;
        }
        else
        {
            gasInput = 0;
        }
    }
    public void OnBrake(InputValue button)
    {
        if(button.isPressed)
        {
            brakeInput = 1;
        }
        else
        {
            brakeInput = 0;
        }
    }
    public void OnSteering(InputValue value)
    {
        steeringInput = value.Get<Vector2>();
    }
   
    public void OnGasEvent(float amount)
    {
        character.GasRecovery(amount);
    }
    public void OnHPEvent(float amount)
    {
        character.HPRecovery(amount);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Chaser"))
        {
            character.TakeDamage(other.GetComponent<Chasing>().Damage);
        }
        /*if(other.CompareTag("Gas"))
        {
            Gas += other.GetComponent<GasCollected>().Gas;
            other.GetComponent<GasCollected>().GasCollect();
        }*/
    }
    private void OnloadEvent(GameSceneSO sceneToLoad, Vector3 posToGo, bool fadeScreen)
    {
        
    }
    private void OnAfterSceneLoadedEvent()
    {
        
    }
    private void OnloadDataEvent()
    {
        
    }
}
