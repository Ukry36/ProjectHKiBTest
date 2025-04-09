using UnityEngine;
using UnityEngine.U2D.Animation;
using System;
using System.Collections.Generic;
using UnityEditor.Animations;
[Serializable]
public class Player : Entity, IAttackable, IDodgeable, IGraffitiable, ISkinable, IStateControllable
{

    #region field
    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public int LastAttackNum { get; set; }
    [field: SerializeField] public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }

    public GameObject yay;
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

    public CustomVariable<float> DodgeCooltime { get; set; }
    public CustomVariable<float> ContinuousDodgeLimit { get; set; }
    public CustomVariable<float> KeepDodgeMaxTime { get; set; }
    public CustomVariable<float> KeepDodgeMaxDistance { get; set; }

    public StatContainer MaxGP { get; set; }
    public StatContainer GP { get; set; }

    public SkinDataSO SkinData { get; set; }

    public StateMachineSO StateMachine { get; set; }
    public AnimatorController AnimatorController { get; set; }

    public MergedPlayerBaseData PlayerBaseData
    {
        get => _playerBaseData;
        set
        {
            _playerBaseData = value;
            UpdateDatas();
        }
    }
    private MergedPlayerBaseData _playerBaseData;


    [SerializeField] private DatabaseManagerSO databaseManager;

    [SerializeField] private MovementManagerSO movementManager;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private StateController stateController;


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

    [SerializeField] private SpriteLibrary spriteLibrary;
    [SerializeField] private SpriteRenderer spriteRenderer;

    #endregion

    public void Initialize()
    {
        UpdateDatas();
    }

    public void UpdateDatas()
    {
        MovePoint.Initialize();
        databaseManager.SetIMovable(this, PlayerBaseData);
        databaseManager.SetIAttackable(this, PlayerBaseData);
        databaseManager.SetIDodgeable(this, PlayerBaseData);
        databaseManager.SetIDamagable(this, PlayerBaseData);
        databaseManager.SetIDodgeable(this, PlayerBaseData);
        databaseManager.SetGraffiriable(this, PlayerBaseData);
        databaseManager.SetISkinable(this, PlayerBaseData);
        databaseManager.SetIStateControllable(this, PlayerBaseData);
    }

    public override void Damage(DamageDataSO damageData, IAttackable hitter)
    {
        throw new NotImplementedException();
    }

    public Vector3 GetAttackOrigin()
    => this.transform.position;


    public void SetGear(MergedPlayerBaseData realGear)
    {
        PlayerBaseData = realGear;
        Initialize();
        SetAnimationController();
        SetStateController();
        SetFootStepController();
        SetAttackController();
        SetSkin();
    }

    private void SetSkin()
    => SkinData.SetSKin(spriteLibrary, AnimatorController, spriteRenderer);


    private void SetAnimationController()
    => animationController.animator.runtimeAnimatorController = AnimatorController;

    private void SetStateController()
    {
        stateController.Initialize(StateMachine);
        stateController.RegisterInterface<IMovable>(this);
        stateController.RegisterInterface<IAttackable>(this);
    }

    private void SetFootStepController()
    => FootstepController.ChangeDefaultFootStepAudio(FootStepAudio);

    private void SetAttackController()
    => AttackController.SetAttacker(this);


}
