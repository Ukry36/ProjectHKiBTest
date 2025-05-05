using System;
using UnityEngine;

public class DamagableObject : Entity, IAttackable
{
    private bool dieWhenKnockBack;
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;
    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public int LastAttackNum { get; set; }
    public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    [field: SerializeField] public DamagableObjectDataSO BaseData { get; set; }
    [SerializeField] private DatabaseManagerSO databaseManager;
    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (BaseData)
            UpdateDatas();
    }

    public void UpdateDatas()
    {
        MovePoint.Initialize();
        databaseManager.SetIMovable(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        this.dieWhenKnockBack = BaseData.DieWhenKnockBack;
    }

    public override void Damage(DamageDataSO damageData, IAttackable hitter)
    {
        if (damageData.knockBack > Mass && dieWhenKnockBack)
        {
            Die();
            return;
        }

        base.Damage(damageData, hitter);
    }

    public void Update()
    {
        movementManager.FollowMovePointIdle(transform, this);
    }
}