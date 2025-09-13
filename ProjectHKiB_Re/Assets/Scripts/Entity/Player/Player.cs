using UnityEngine;
using UnityEngine.U2D.Animation;
using System;
using UnityEditor.Animations;
[Serializable]
public class Player : Entity, IAttackable, IDodgeable, IGraffitiable, ISkinable, IEntityStateControllable, ITargetable
{
    #region field
    public int BaseATK { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    [field: SerializeField] public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

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
    public float BaseDodgeCooltime { get; set; }
    public float InitialDodgeMaxDistance { get; set; }
    public float BaseDodgeSpeed { get; set; }
    public int BaseContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public float BaseKeepDodgeMaxTime { get; set; }
    public float BaseDodgeInvincibleTime { get; set; }
    [field: SerializeField] public DodgeController DodgeController { get; set; }
    public ParticlePlayer KeepDodgeParticle { get; set; }

    public int MaxGP { get; set; }
    public int GP { get; set; }

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
    [field: SerializeField] public DirAnimationController AnimationController { get; set; }
    [field: SerializeField] public StateController StateController { get; set; }
    [field: SerializeField] public TargetController TargetController { get; set; }
    public StatBuffCompilation JustDodgeBuff { get; set; }

    // height based movement test!!!

    [SerializeField] private SpriteLibrary spriteLibrary;
    [SerializeField] private SpriteRenderer spriteRenderer;

    #endregion


    public override void Initialize()
    {
        UpdateDatas();
        base.Initialize();
        SetStateController();
    }

    public void UpdateDatas()
    {
        databaseManager.SetIMovable(this, PlayerBaseData);
        databaseManager.SetIAttackable(this, PlayerBaseData);
        databaseManager.SetIDamagable(this, PlayerBaseData);
        databaseManager.SetIDodgeable(this, PlayerBaseData);
        databaseManager.SetGraffiriable(this, PlayerBaseData);
        databaseManager.SetISkinable(this, PlayerBaseData);
        databaseManager.SetIStateControllable(this, PlayerBaseData);
        databaseManager.SetITargetable(this, PlayerBaseData);
    }

    public void SetGear(MergedPlayerBaseData realGear)
    {
        PlayerBaseData = realGear;
        Initialize();
        SetStateController();
        SetBuffController();
        FootstepController.ChangeDefaultFootStepAudio(FootStepAudio);
        AttackController.SetAttacker(this);
        SkinData.SetSKin(spriteLibrary, AnimatorController, spriteRenderer);
        AnimationController.Initialize(AnimatorController);
    }

    private void SetStateController()
    {
        StateController.RegisterInterface<IMovable>(this);
        StateController.RegisterInterface<IAttackable>(this);
        StateController.RegisterInterface<IDirAnimatable>(this);
        StateController.RegisterInterface<IDodgeable>(this);
        StateController.RegisterInterface<ITargetable>(this);
        StateController.RegisterInterface<IBuffable>(this);
        StateController.Initialize(StateMachine);
    }

    private void SetBuffController()
    {
        StatBuffController.RegisterInterface<IMovable>(this);
        StatBuffController.RegisterInterface<IAttackable>(this);
        StatBuffController.RegisterInterface<IDodgeable>(this);
        StatBuffController.RegisterInterface<IDamagable>(this);
    }
}
