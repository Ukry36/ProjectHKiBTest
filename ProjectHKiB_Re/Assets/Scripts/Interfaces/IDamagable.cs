using System;
using UnityEngine;
using UnityEngine.Events;

public interface IDamagable
{
    public int BaseMaxHP { get; set; }
    public FloatBuffContainer MaxHPBuffer { get; set; }
    public int MaxHP { get => (int)MaxHPBuffer.BuffedStat; }
    public EventHandler<float> OnMaxHPChanged { get => MaxHPBuffer.OnBuffed; }

    public int HP { get; set; }
    public EventHandler<int> OnHPChanged { get; set; }

    public int BaseDEF { get; set; }
    public FloatBuffContainer DEFBuffer { get; set; }
    public int DEF { get => (int)DEFBuffer.BuffedStat; }
    public EventHandler<float> OnDEFChanged { get => DEFBuffer.OnBuffed; }

    public FloatBuffContainer ResistanceBuffer { get; set; }
    public float Resistance { get => ResistanceBuffer.BuffedStat; }
    public EventHandler<float> OnResistanceChanged { get => ResistanceBuffer.OnBuffed; }

    public BoolBuffContainer InvincibleBuffer { get; set; }
    public bool Invincible { get => InvincibleBuffer.BuffedStat; }
    public EventHandler<bool> OnInvincibleChanged { get => InvincibleBuffer.OnBuffed; }

    public BoolBuffContainer SuperArmourBuffer { get; set; }
    public bool SuperArmour { get => SuperArmourBuffer.BuffedStat; }
    public EventHandler<bool> OnSuperArmourChanged { get => SuperArmourBuffer.OnBuffed; }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    => HealthController.Damage(damageData, hitter, origin, this);
    public void Die()
    => HealthController.Die();
    public void Heal(int amount)
    => HealthController.Heal(amount, MaxHP);

    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
    public HealthController HealthController { get; set; }
}