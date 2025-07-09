using System;
using UnityEngine;

public class DamagableObject : Entity, IAttackable
{
    private bool dieWhenKnockBack;
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;
    public int BaseATK { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    [field: SerializeField] public DamagableObjectDataSO BaseData { get; set; }
    [SerializeField] private DatabaseManagerSO databaseManager;
    public void Start()
    {
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();
        if (BaseData)
            UpdateDatas();
    }

    public void UpdateDatas()
    {
        databaseManager.SetIMovable(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        this.dieWhenKnockBack = BaseData.DieWhenKnockBack;
    }

    [SerializeField] private MovementManagerSO movementManager;
    public void Update()
    {
        movementManager.FollowMovePointIdle(transform, this);
    }
}