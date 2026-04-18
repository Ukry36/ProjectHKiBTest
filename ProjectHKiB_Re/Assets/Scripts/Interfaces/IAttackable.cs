using System;
using UnityEngine.U2D.Animation;

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
    public int ATK { get => (int)ATKBuffer.BuffedStat; }
    public Action<float> OnATKChanged { get => ATKBuffer.OnBuffed; }
    public bool IsAttackCooltime { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
    public int AttackNumber { get; set; }

    public void SetAttacker();
    public void StartAttackCooltime();
    public void SetAttackData(int attackNumber);

    public void Attack(int damageNumber);

    public void StopEffect(int animPlayerNum);
}