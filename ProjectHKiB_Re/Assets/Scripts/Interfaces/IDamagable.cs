using System;
using UnityEngine;

public interface IDamagableBase
{
    public float BaseMaxHP { get; set; }
    public float BaseDEF { get; set; }

    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
}

public interface IDamagable : IDamagableBase, IInitializable
{
    public FloatBuffContainer MaxHPBuffer { get; set; }
    public float MaxHP { get => MaxHPBuffer.GetBuffedStat(BaseMaxHP, 0); }
    public float HP { get; set; }
    public Action<float> OnHPChanged { get; set; }

    public FloatBuffContainer DEFBuffer { get; set; }
    public float DEF { get => DEFBuffer.GetBuffedStat(BaseDEF, 0); }
    public Action<float> OnDEFChanged { get; set; }

    public FloatBuffContainer ResistanceBuffer { get; set; }
    public float Resistance { get => ResistanceBuffer.GetBuffedStat(0); }
    public Action<float> OnResistanceChanged { get; set; }

    public BoolBuffContainer InvincibleBuffer { get; set; }
    public bool Invincible { get => InvincibleBuffer.GetBuffedStat(0, isNegative: false); }
    public Action<bool> OnInvincibleChanged { get; set; }

    public BoolBuffContainer SuperArmourBuffer { get; set; }
    public bool SuperArmour { get => SuperArmourBuffer.GetBuffedStat(0, isNegative: false); }
    public Action<bool> OnSuperArmourChanged { get; set; }

    public Action OnDamaged { get; set; }
    public Action OnDie { get; set; }
    public Action OnHealed { get; set; }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin);
    public void Die();
    public void Heal(int amount);

}