using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Friendly : Entity, IAttackable, IPoolable
{
    public int ID { get; set; }
    public int BaseATK { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    public AttackController AttackController { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public int PoolSize { get; set; }
    public UnityEvent<int, int> OnGameObjectDisabled { get; set; }

    public FriendlyDataSO friendlyBaseData;

    public override void Initialize()
    {
        base.Initialize();
        if (friendlyBaseData)
            InitializeFromPool(friendlyBaseData);
    }

    public void InitializeFromPool(FriendlyDataSO friendlyData)
    {
        friendlyBaseData = friendlyData;
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(friendlyBaseData.GetInstanceID(), this.gameObject.GetHashCode());
    }
}
