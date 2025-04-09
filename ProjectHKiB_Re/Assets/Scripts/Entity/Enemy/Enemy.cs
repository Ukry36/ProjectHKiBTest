using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity, IAttackable, IPoolable
{
    public AttackDataSO[] AttackDatas { get; set; }
    public StatContainer ATK { get; set; }
    public int LastAttackNum { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }

    public AttackController AttackController { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public int PoolSize { get; set; }

    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;

    public EnemyDataSO enemyBaseData;

    public void Initialize()
    {
        MovePoint.Initialize();
        if (enemyBaseData)
            InitializeFromPool(enemyBaseData);
    }

    public void InitializeFromPool(EnemyDataSO enemyData)
    {
        enemyBaseData = enemyData;
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(enemyBaseData.GetInstanceID(), this.gameObject.GetHashCode());
    }

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public override void Damage(DamageDataSO damageData, IAttackable hitter)
    {
        throw new System.NotImplementedException();
    }
}