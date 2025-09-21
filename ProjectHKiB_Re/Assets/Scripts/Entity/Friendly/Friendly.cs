using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Friendly : Entity, IPoolable
{
    public int ID { get; set; }

    public int PoolSize { get; set; }
    public UnityEvent<int, int> OnGameObjectDisabled { get; set; }

    public FriendlyDataSO BaseData;
    [SerializeField] private DatabaseManagerSO databaseManager;
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

    public void InitializeFromPool(FriendlyDataSO friendlyData)
    {
        BaseData = friendlyData;
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(BaseData.GetInstanceID(), this.gameObject.GetHashCode());
    }
}
