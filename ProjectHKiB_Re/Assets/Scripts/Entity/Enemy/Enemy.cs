using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
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
    public override void Awake()
    {
        base.Awake();
        GetInterface<IDamagable>().OnDie += OnDie;
    }
    protected void OnDestroy()
    {
        GetInterface<IDamagable>().OnDie -= OnDie;
    }

    public override void Initialize()
    {
        base.Initialize();
        databaseManager.SetIMovable(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        databaseManager.SetIFootstep(this, BaseData);
        databaseManager.SetIPathFindable(this, BaseData);
        databaseManager.SetIAnimatable(this, BaseData);
        Initialize(BaseData.StateMachine);
        GetInterface<IMovable>().Initialize();
        GetInterface<IAttackable>()?.Initialize();
        GetInterface<IDamagable>()?.Initialize();
        GetInterface<IFootstep>()?.Initialize();
        GetInterface<IFootstep>()?.Initialize();
        GetInterface<ISkinable>()?.ApplySkin(BaseData.AnimatorController);
        GetInterface<ITargetable>()?.Initialize();
        GetInterface<IAnimatable>()?.Initialize();
    }

    public void InitializeFromPool(EnemyDataSO enemyData)
    {
        BaseData = enemyData;
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