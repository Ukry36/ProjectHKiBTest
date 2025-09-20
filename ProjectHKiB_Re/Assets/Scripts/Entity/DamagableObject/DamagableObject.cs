using System;
using UnityEngine;

public class DamagableObject : Entity
{
    private bool dieWhenKnockBack;
    [field: SerializeField] public DamagableObjectDataSO BaseData { get; set; }
    [SerializeField] private DatabaseManagerSO databaseManager;
    public void Start()
    {
        Initialize();
    }

    public override void Initialize()
    {
        if (BaseData)
            UpdateDatas();
        base.Initialize();
    }

    public void UpdateDatas()
    {
        databaseManager.SetIMovable(this, BaseData);
        //databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        this.dieWhenKnockBack = BaseData.DieWhenKnockBack;
    }

    [SerializeField] private MovementManagerSO movementManager;
    public void Update()
    {
        movementManager.FollowMovePointIdle(transform, this);
    }
}