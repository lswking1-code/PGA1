using UnityEngine;
using UnityEngine.Events;
public class Character : MonoBehaviour
{
    [Header("EventListeners")]
    public VoidEventSO NewGameEvent;
    public VoidEventSO LoadDataEvent;
    public VoidEventSO AfterSceneLoadedEvent;
   [Header("Attributes")]
    public float HP = 100;
    public float MaxHP = 100;
    public float Armor = 100;
    public float MaxArmor = 100;
    public float Gas = 100;
    public float MaxGas = 100;
    [Range(0, 10)]
    public float GasConsumption = 1f;
    public bool GasConsumptionEnabled = true;

    public UnityEvent<Character> OnHealthChange;

    public UnityEvent OnTakeDamage;
    public UnityEvent OnDie;

    public UnityEvent<Character> OnGasRecovery;

    
    private void OnEnable()
    {
        NewGameEvent.OnEventRaised += OnNewGameEvent;
        LoadDataEvent.OnEventRaised += OnLoadDataEvent;
        AfterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadedEvent;
    }
    private void OnDisable()
    {
        NewGameEvent.OnEventRaised -= OnNewGameEvent;
        LoadDataEvent.OnEventRaised -= OnLoadDataEvent; 
        AfterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
    }
    private void Start()
    {
        HP = MaxHP;
        Armor = MaxArmor;
        Gas = MaxGas;
        OnHealthChange.Invoke(this);
    }
    private void FixedUpdate()
    {
        if(GasConsumptionEnabled)
        {
           
            Gas -= GasConsumption * Time.deltaTime;
            if(Gas == 0)
            {
                OnDie.Invoke();
                Debug.Log("Gas is 0");
            }
        }
    }
    public void TakeDamage(float damage)
    {
        if(HP - damage > 0)
        {
            HP -= damage;
        }
        else
        {
            HP = 0;
            OnDie.Invoke();
            Debug.Log("HP is 0");
        }
        OnHealthChange.Invoke(this);
    }
    public void GasRecovery(float amount)
    {
        if(Gas + amount > MaxGas)
        {
            Gas = MaxGas;
        }
        else
        {
            Gas += amount;
        }
        OnGasRecovery.Invoke(this);
    }
    public void HPRecovery(float amount)
    {
        if(HP + amount > MaxHP)
        {
            HP = MaxHP;
        }
        else
        {
            HP += amount;
        }
        OnHealthChange.Invoke(this);
    }
    private void OnNewGameEvent()
    {
        HP = MaxHP;
        Armor = MaxArmor;
        Gas = MaxGas;
        OnHealthChange.Invoke(this);
    }
    private void OnLoadDataEvent()
    {
        HP = MaxHP;
        Armor = MaxArmor;
        Gas = MaxGas;
        OnHealthChange.Invoke(this);
    }   
    private void OnAfterSceneLoadedEvent()
    {
        HP = MaxHP;
        Armor = MaxArmor;
        Gas = MaxGas;
        OnHealthChange.Invoke(this);
    }
}
