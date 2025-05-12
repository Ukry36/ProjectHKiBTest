using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Friendly : Entity, IAttackable, IPoolable
{
    public StatContainer ATK { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    public AttackController AttackController { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public int PoolSize { get; set; }
    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;

    public FriendlyDataSO friendlyBaseData;

    public void Initialize()
    {
        MovePoint.Initialize();
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

    public Vector3 GetAttackOrigin()
    {
        throw new NotImplementedException();
    }

    public override void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    {
        throw new NotImplementedException();
    }
}
