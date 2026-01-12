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
    public VoidEventSO TimeoutEvent;
    public ResourceEventSO GasEvent;
    public ResourceEventSO HPEvent;
    [Header("EventRaise")]
    public VoidEventSO SaveDataEvent;
    [Header("Attributes")]
    private Character character;
    
    [Header("Input")]
    public float gasInput;
    public float brakeInput;
    public Vector2 steeringInput;
    public PlayerInput inputControl;
    
    [Header("Physics")]
    private Drive drive;
    private Rigidbody rb;
    private float horizontalInput;
    private float forwardInput;
    public bool isDead = false;




    private void OnEnable()
    {
        sceneloadEvent.LoadRequestEvent += OnloadEvent;
        afterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadedEvent;
        LoadDataEvent.OnEventRaised += OnloadDataEvent;
        backToMenuEvent.OnEventRaised += OnloadDataEvent;
        GasEvent.OnEventRaised += OnGasEvent;
        HPEvent.OnEventRaised += OnHPEvent;
        TimeoutEvent.OnEventRaised += OnTimeoutEvent;
    }
    private void OnDisable()
    {
        sceneloadEvent.LoadRequestEvent -= OnloadEvent;
        afterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
        LoadDataEvent.OnEventRaised -= OnloadDataEvent;
        backToMenuEvent.OnEventRaised -= OnloadDataEvent;
        GasEvent.OnEventRaised -= OnGasEvent;
        HPEvent.OnEventRaised -= OnHPEvent;
        TimeoutEvent.OnEventRaised -= OnTimeoutEvent;
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        character = GetComponent<Character>();
        drive = GetComponent<Drive>();
        inputControl = GetComponent<PlayerInput>();
    }



    private void FixedUpdate()
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
        inputControl.actions.FindActionMap("Drive")?.Disable();
    }
    private void OnAfterSceneLoadedEvent()
    {
        inputControl.actions.FindActionMap("Drive")?.Enable();
        SaveDataEvent.RaiseEvent();
    }
    private void OnloadDataEvent()
    {
        isDead = false;
        inputControl.actions.FindActionMap("Drive")?.Enable();
    }
    public void PlayerDeath()
    {
        inputControl.actions.FindActionMap("Drive")?.Disable();
        isDead = true;
        gasInput = 0;
        brakeInput = 0;
        steeringInput = Vector2.zero; 
    }
    private void OnTimeoutEvent()
    {
        inputControl.actions.FindActionMap("Drive")?.Disable();
        gasInput = 0;
        brakeInput = 0;
        steeringInput = Vector2.zero;
    }
}
