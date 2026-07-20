using Assets.Scripts.Interfaces.Modules;
using UnityEngine;
using UnityEngine.Events;
public class Enemy : Entity, IPoolable
{
    [field: SerializeField] public int PoolSize { get; set; }

    public int LastAttackIndicatorID { get; set; }
    public UnityEvent<int, int> OnGameObjectDisabled { get; set; } = new();
    public int ID { get; set; }

    public EnemyDataSO BaseData;
    [SerializeField] private DatabaseManagerSO databaseManager;
    public override void Start()
    {
        base.Start();
        GetInterface<IDamagable>().OnDie += OnDie;
    }
    protected void OnDestroy()
    {
        if (TryGetInterface(out IDamagable damagable))
        {
            damagable.OnDie -= OnDie;
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        if (BaseData == null)
        {
            Debug.Log("BaseData is Null");
            return;
        }
        databaseManager.SetIPhysics(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        databaseManager.SetIFootstep(this, BaseData);
        databaseManager.SetIPathFindable(this, BaseData);
        databaseManager.SetIDirAnimatable(this, BaseData);
        databaseManager.SetITargetable(this, BaseData);
        //databaseManager.SetISkinable(this, BaseData);
        Initialize(BaseData.StateMachine);
        InitializeModules();
    }

    public void InitializeFromPool(EnemyDataSO enemyData)
    {
        BaseData = enemyData;
        Initialize();
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(ID, this.gameObject.GetHashCode());
    }

    public void OnDie()
    {
        if (LastAttackIndicatorID != 0)
            GameManager.instance.attackAreaIndicatorManager.StopIndicating(LastAttackIndicatorID);

    }
}