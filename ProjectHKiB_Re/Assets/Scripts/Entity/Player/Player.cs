using UnityEngine;
using UnityEngine.U2D.Animation;
using System;
using UnityEditor.Animations;
[Serializable]
public class Player : Entity, IGraffitiable
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

    public int MaxGP { get; set; }
    public int GP { get; set; }

    public MergedPlayerBaseData BaseData
    {
        get => _playerBaseData;
        set { _playerBaseData = value; }
    }
    private MergedPlayerBaseData _playerBaseData;

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
        databaseManager.SetIMovable(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        databaseManager.SetIDodgeable(this, BaseData);
        databaseManager.SetIFootstep(this, BaseData);
        databaseManager.SetGraffitiable(this, BaseData);
        databaseManager.SetISkinable(this, BaseData);
        databaseManager.SetITargetable(this, BaseData);
        databaseManager.SetIAnimatable(this, BaseData);
        Initialize(BaseData.StateMachine);
        GetInterface<IMovable>()?.Initialize();
        GetInterface<IAttackable>()?.Initialize();
        GetInterface<IDamagable>()?.Initialize();
        GetInterface<IDodgeable>()?.Initialize();
        GetInterface<IFootstep>()?.Initialize();
        //graffiti
        GetInterface<IFootstep>()?.Initialize();
        GetInterface<ISkinable>()?.ApplySkin(BaseData.AnimatorController);
        GetInterface<ITargetable>()?.Initialize();
        GetInterface<IAnimatable>()?.Initialize();
    }

    public void SetGear(MergedPlayerBaseData mergedGear)
    {
        BaseData = mergedGear;
        Initialize();
    }

}
