using System.Collections.Generic;
using UnityEngine;

public class Enemy : StateController, IGridMovable, IAttackable, IDamagable, IPoolable
{
    public MovePoint MovePoint { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }
    public CustomVariable<float> ATK { get; set; }
    public CustomVariable<float> CriticalChanceRate { get; set; }
    public CustomVariable<float> CriticalDamageRate { get; set; }
    public CustomVariable<float> MaxHP { get; set; }
    public CustomVariable<float> HP { get; set; }
    public CustomVariable<float> DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public CustomVariable<float> Speed { get; set; }
    public CustomVariable<float> SprintCoeff { get; set; }
    public int PoolSize { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }
    public LayerMask WallLayer { get; set; }

    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;

    public EnemyDataSO enemyBaseData;

    public void Initialize()
    {
        MovePoint.Initialize();
        InitializeState(enemyBaseData.stateMachine);
        if (enemyBaseData)
            InitializeFromPool(enemyBaseData);
    }

    public void InitializeFromPool(EnemyDataSO enemyData)
    {
        enemyBaseData = enemyData;
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(enemyBaseData.ID, this.gameObject.GetHashCode());
    }

    public System.Numerics.Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotImplementedException();
    }
}