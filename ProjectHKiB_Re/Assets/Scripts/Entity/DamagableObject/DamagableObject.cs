using System;
using UnityEngine;

public class DamagableObject : Entity
{
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
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
    }

    [SerializeField] private MovementManagerSO movementManager;
    public override void UpdateState()
    {
        base.UpdateState();
        movementManager.FollowMovePointIdle(transform, GetInterface<IMovable>());
    }
}