using UnityEngine;
using Assets.Scripts.Interfaces.Modules;

public class Player : Entity
{
    #region field

    public GameObject yay;
    /*
    public void Update()
    {
        if (CurrentTarget)
        {
            yay.SetActive(true);
            yay.transform.position = CurrentTarget.position;
        }
        else
        {
            yay.SetActive(false);
        }
    }
    */

    public PlayerBaseDataSO BaseData;

    [SerializeField] private DatabaseManagerSO databaseManager;

    // height based movement test!!!
    [SerializeField] private Transform sprite;
    public float Height
    {
        get => sprite.localPosition.y;
        set
        {
            sprite.localPosition = Vector3.up * value;
            Caninteract = value < canInteractHeight;
        }
    }
    [SerializeField] private float canInteractHeight;
    public bool Caninteract { get; private set; }

    // height based movement test!!!

    #endregion

    public override void Initialize()
    {
        base.Initialize();
        if (BaseData == null)
        {
            Debug.Log("BaseData is Null");
            return;
        }
        databaseManager.SetIPhysics(this, BaseData);
        if (TryGetInterface(out IAttackable attackable)) databaseManager.SetIAttackable(attackable, BaseData);
        if (TryGetInterface(out IDamagable damagable)) databaseManager.SetIDamagable(damagable, BaseData);
        if (TryGetInterface(out IDodgeable dodgeable)) databaseManager.SetIDodgeable(dodgeable, BaseData);
        databaseManager.SetIFootstep(this, BaseData);
        databaseManager.SetISkinable(this, BaseData);
        if (TryGetInterface(out ITargetable targetable)) databaseManager.SetITargetable(targetable, BaseData);
        databaseManager.SetIDirAnimatable(this, BaseData);
        if (TryGetInterface(out IGraffitiable graffitiable)) databaseManager.SetIGraffitiable(graffitiable, BaseData);
        InitializeStateMachine(BaseData.StateMachine);
        InitializeModules();
    }

    public void SetGear(PlayerBaseDataSO mergedGear)
    {
        BaseData = mergedGear;
        Initialize();
    }

}
