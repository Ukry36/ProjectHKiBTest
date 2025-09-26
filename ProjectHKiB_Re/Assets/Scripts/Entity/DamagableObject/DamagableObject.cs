using Assets.Scripts.Interfaces.Modules;
using UnityEngine;
[RequireComponent(typeof(AttackableModule))]
[RequireComponent(typeof(MovableModule))]
[RequireComponent(typeof(DamagableModule))]
public class DamagableObject : Entity
{
    [field: SerializeField] public DamagableObjectDataSO BaseData { get; set; }
    [SerializeField] private DatabaseManagerSO databaseManager;
    public override void Start()
    {
        base.Start();
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();
        if (BaseData == null)
        {
            Debug.Log("BaseData is Null");
            return;
        }
        databaseManager.SetIMovable(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        InitializeModules();
    }


    [SerializeField] private MovementManagerSO movementManager;
    public override void UpdateState()
    {
        base.UpdateState();
        movementManager.FollowMovePointIdle(transform, GetInterface<IMovable>());
    }
}