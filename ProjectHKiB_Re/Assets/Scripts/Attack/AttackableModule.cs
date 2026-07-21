using System;
using UnityEngine;
using UnityEngine.U2D.Animation;
using Random = UnityEngine.Random;

public interface IAttackableBase
{
    public int BaseATK { get; set; }

    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public SimpleAnimationDataSO EffectAnimationData { get; set; }
    public SpriteLibraryAsset EffectSpriteLibrary { get; set; }
}

public interface IAttackable : IAttackableBase, IInitializable
{
    public FloatBuffContainer ATKBuffer { get; set; }
    public int ATK { get => (int)ATKBuffer.GetBuffedStat(BaseATK, 0); }
    public Action<float> OnATKChanged { get; set; }
    public bool IsAttackCooltime { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
    public int AttackNumber { get; set; }

    public void SetAttacker();
    public void StartAttackCooltime();
    public void SetAttackData(int attackNumber);

    public void Attack(int damageNumber);

    public void StopEffect(int animPlayerNum);
}

public class AttackableModule : InterfaceModule, IAttackable
{
    public int BaseATK { get; set; }
    public FloatBuffContainer ATKBuffer { get; set; }

    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }

    [SerializeField] private Damager damager;
    public bool IsAttackCooltime { get; set; }
    public int AttackNumber { get; set; }
    public Transform CurrentTarget { get; set; }
    public SimpleAnimationDataSO EffectAnimationData { get; set; }
    public SpriteLibraryAsset EffectSpriteLibrary { get; set; }

    public Timer attackCooltime;

    public CooltimeMultiplierBuffContainer AttackCooltimeBuffer { get; private set; }

    private float _currentBaseAttackCooltime;

    public float AttackCooltimeMultiplier =>
        AttackCooltimeBuffer != null ? AttackCooltimeBuffer.BuffedMultiplier : 1f;

    //Accuracy Debuff Method
    public FloatBuffContainer AccuracyMissChanceBuffer { get; private set; }

    public float AccuracyMissChance =>
        AccuracyMissChanceBuffer != null ? Mathf.Clamp01(AccuracyMissChanceBuffer.GetBuffedStat(0)) : 0f;

    public bool HasRolledAccuracyDebuffThisAttack { get; private set; }
    public bool IsAccuracyDirDistortedThisAttack { get; private set; }

    //SelfDamage Debuff Method
    public FloatBuffContainer SelfDamageChanceBuffer { get; private set; }

    public float SelfDamageChance =>
        SelfDamageChanceBuffer != null ? Mathf.Clamp01(SelfDamageChanceBuffer.GetBuffedStat(0)) : 0f;

    public bool IsSelfDamageTriggered { get; private set; }
    public int PendingSelfDamageDamageNumber { get; private set; }

    // Groggy / Runaway Debuff Method
    public BoolBuffContainer GroggyBuffer { get; private set; }
    public BoolBuffContainer RunawayBuffer { get; private set; }

    public bool IsGroggy =>
        GroggyBuffer != null && GroggyBuffer.GetBuffedStat(0, isNegative: true);

    public bool IsRunaway =>
        RunawayBuffer != null && RunawayBuffer.GetBuffedStat(0, isNegative: true);

    public System.Action<float> OnATKChanged { get; set; }

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IAttackable>(this);
    }

    public virtual void Initialize()
    {
        ATKBuffer = new();
        AttackCooltimeBuffer = new(1f);
        AccuracyMissChanceBuffer = new();
        SelfDamageChanceBuffer = new();
        AttackCooltimeBuffer.OnBuffed += OnAttackCooltimeBuffChanged;

        GroggyBuffer = new();
        RunawayBuffer = new();

        SetAttacker();
        attackCooltime = new();
        IsAttackCooltime = false;
        _currentBaseAttackCooltime = 0f;

        ResetAccuracyDebuffAttackState();
        ResetSelfDamageState();

        if (damager != null)
            damager.SetAnimationData(EffectAnimationData, EffectSpriteLibrary);
    }

    public void ResetAccuracyDebuffAttackState()
    {
        HasRolledAccuracyDebuffThisAttack = false;
        IsAccuracyDirDistortedThisAttack = false;
    }

    public bool TryRollAccuracyDebuff()
    {
        if (HasRolledAccuracyDebuffThisAttack)
            return IsAccuracyDirDistortedThisAttack;

        HasRolledAccuracyDebuffThisAttack = true;
        IsAccuracyDirDistortedThisAttack = Random.value < AccuracyMissChance;
        return IsAccuracyDirDistortedThisAttack;
    }

    public void SetAttacker()
    {
        if (damager == null) return;
        damager.SetAttackable(this);
    }

    public void StartAttackCooltime()
    {
        IsAttackCooltime = true;

        _currentBaseAttackCooltime = AttackDatas[AttackNumber].coolTime;
        float finalCooltime = _currentBaseAttackCooltime * AttackCooltimeMultiplier;

        attackCooltime.StartTimer(finalCooltime, () => IsAttackCooltime = false);
    }

    private void OnAttackCooltimeBuffChanged(float multiplier)
    {
        if (attackCooltime == null || attackCooltime.IsCooltimeEnded) return;
        if (_currentBaseAttackCooltime <= 0f) return;

        float newTotalCooltime = _currentBaseAttackCooltime * multiplier;
        attackCooltime.RecalculateTotalTime(newTotalCooltime);
    }

    public void ResetSelfDamageState()
    {
        IsSelfDamageTriggered = false;
        PendingSelfDamageDamageNumber = 0;
    }

    public void RollSelfDamage(int damageNumber)
    {
        PendingSelfDamageDamageNumber = damageNumber;
        IsSelfDamageTriggered = Random.value < SelfDamageChance;
    }

    public void ExecuteSelfDamage()
    {
        if (AttackDatas == null)
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!");
            return;
        }

        if (AttackNumber < 0 || AttackNumber >= AttackDatas.Length)
        {
            Debug.LogError($"ERROR: AttackData[{AttackNumber}] is missing!!!");
            return;
        }

        if (AttackDatas[AttackNumber].damageDatas == null ||
            PendingSelfDamageDamageNumber < 0 ||
            PendingSelfDamageDamageNumber >= AttackDatas[AttackNumber].damageDatas.Length)
        {
            Debug.LogError($"ERROR: DamageData[{PendingSelfDamageDamageNumber}] is missing!!!");
            return;
        }

        if (TryGetComponent(out IDamagable selfDamagable))
        {
            DamageDataSO selfDamageData = AttackDatas[AttackNumber].damageDatas[PendingSelfDamageDamageNumber];
            selfDamagable.Damage(selfDamageData, this, transform.position);
        }
        else
        {
            Debug.LogError("ERROR: Self damage failed - IDamagable not found.");
        }

        ResetSelfDamageState();
    }

    public void SetAttackData(int attackNumber)
    {
        if (AttackDatas == null)
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
        }

        ResetAccuracyDebuffAttackState();

        DamageIndicatorRandomPosInfo = Random.value;

        if (attackNumber < AttackDatas.Length)
            AttackNumber = attackNumber;
        else
            AttackNumber = 0;
    }

    public void Attack(int damageNumber)
    {
        if (!damager)
        {
            Debug.LogError("ERROR: Damager is missing!!!"); return;
        }
        damager.SetDamageData(AttackNumber, damageNumber);
        damager.Damage();
    }

    public void StopEffect(int animPlayerNum)
    {
        damager.StopEffect(animPlayerNum);
    }
}