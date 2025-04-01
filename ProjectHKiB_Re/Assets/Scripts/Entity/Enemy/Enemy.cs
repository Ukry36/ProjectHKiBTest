using System.Collections.Generic;
using UnityEngine;

public class Enemy : StateController, IMovable, IAttackable, IDamagable, IPoolable
{
    public MovePoint MovePoint { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }
    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    public StatContainer DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public StatContainer Speed { get; set; }
    public StatContainer SprintCoeff { get; set; }
    public int PoolSize { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }
    public LayerMask WallLayer { get; set; }
    public bool IsSprinting { get; set; } = false;
    public AudioDataSO FootStepAudio { get; set; }

    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;

    public EnemyDataSO enemyBaseData;

    public void Initialize()
    {
        MovePoint.Initialize();
        Initialize(enemyBaseData.stateMachine);
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

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotImplementedException();
    }
}