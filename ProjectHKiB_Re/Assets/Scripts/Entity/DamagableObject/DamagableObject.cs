using Assets.Scripts.Interfaces.Modules;
using UnityEngine;
public class DamagableObject : Entity
{
    [field: SerializeField] public DamagableObjectDataSO BaseData { get; set; }
    [SerializeField] private DatabaseManagerSO databaseManager;
    public override void Start()
    {
        base.Start();
        //Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();
        if (BaseData == null)
        {
            Debug.Log("BaseData is Null");
            return;
        }
        databaseManager.SetIPhysics(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        InitializeModules();
    }


    public override void UpdateState()
    {
        base.UpdateState();
    }
}