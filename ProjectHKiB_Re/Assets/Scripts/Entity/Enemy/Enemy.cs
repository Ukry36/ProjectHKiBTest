using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class Enemy : Entity, IAttackAreaIndicatable, IPathFindable, IAttackable, IPoolable, IEntityStateControllable
{
    public AttackDataSO[] AttackDatas { get; set; }
    public int BaseATK { get; set; }
    public LayerMask[] TargetLayers { get; set; }

    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    [field: SerializeField] public AttackController AttackController { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public int PoolSize { get; set; }



    public StateMachineSO StateMachine { get; set; }
    public AnimatorController AnimatorController { get; set; }
    [field: SerializeField] public DirAnimationController AnimationController { get; set; }
    [field: SerializeField] public StateController StateController { get; set; }
    public int LastAttackAreaIndicatorID { get; set; }
    public List<Vector3> PathList { get; set; }
    [field: SerializeField] public PathFindController PathFindController { get; set; }
    [field: SerializeField] public TargetController TargetController { get; set; }
    Action<int, int> IPoolable.OnGameObjectDisabled { get; set; }

    public EnemyDataSO enemyBaseData;
    [SerializeField] private DatabaseManagerSO databaseManager;
    protected void Awake()
    {
        HealthController.OnDie += OnDie;
    }
    protected void OnDestroy()
    {
        HealthController.OnDie -= OnDie;
    }

    void Start()
    {
        Initialize();
    }
    public override void Initialize()
    {
        UpdateDatas();
        PathFindController.Initialize(this);
        base.Initialize();
        AttackController.SetAttacker(this);
    }

    public void UpdateDatas()
    {
        databaseManager.SetIMovable(this, enemyBaseData);
        databaseManager.SetIAttackable(this, enemyBaseData);
        databaseManager.SetIDamagable(this, enemyBaseData);
        databaseManager.SetIStateControllable(this, enemyBaseData);
        databaseManager.SetITargetable(this, enemyBaseData);
        SetAnimationController();
        SetStateController();
        SetBuffController();
    }
    private void SetAnimationController()
    => AnimationController.animator.runtimeAnimatorController = AnimatorController;
    private void SetStateController()
    {
        StateController.RegisterInterface<IMovable>(this);
        StateController.RegisterInterface<IAttackable>(this);
        StateController.RegisterInterface<IAttackAreaIndicatable>(this);
        StateController.RegisterInterface<IPathFindable>(this);
        StateController.RegisterInterface<ITargetable>(this);
        StateController.RegisterInterface<IDirAnimatable>(this);
        StateController.RegisterInterface<IBuffable>(this);
        StateController.Initialize(StateMachine);
    }

    private void SetBuffController()
    {
        StatBuffController.RegisterInterface<IMovable>(this);
        StatBuffController.RegisterInterface<IAttackable>(this);
        StatBuffController.RegisterInterface<IDamagable>(this);
    }

    public void InitializeFromPool(EnemyDataSO enemyData)
    {
        enemyBaseData = enemyData;
    }

    public void OnDisable()
    {
        IPoolable.OnGameObjectDisabled?.Invoke(enemyBaseData.GetInstanceID(), this.gameObject.GetHashCode());
    }

    public void OnDie()
    {
        if (LastAttackAreaIndicatorID != 0)
            GameManager.instance.attackAreaIndicatorManager.StopIndicating(LastAttackAreaIndicatorID);

    }
}