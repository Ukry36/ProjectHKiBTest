using System;
using UnityEngine;

public interface IDamagable : IDamagableBase, IInitializable
{
    public FloatBuffContainer MaxHPBuffer { get; set; }
    public float MaxHP { get => MaxHPBuffer.BuffedStat; }
    public Action<float> OnMaxHPChanged { get => MaxHPBuffer.OnBuffed; set => MaxHPBuffer.OnBuffed = value; }

    public float HP { get; set; }
    public Action<float> OnHPChanged { get; set; }

    public FloatBuffContainer DEFBuffer { get; set; }
    public float DEF { get => DEFBuffer.BuffedStat; }
    public Action<float> OnDEFChanged { get => DEFBuffer.OnBuffed; }

    public FloatBuffContainer ResistanceBuffer { get; set; }
    public float Resistance { get => ResistanceBuffer.BuffedStat; }
    public Action<float> OnResistanceChanged { get => ResistanceBuffer.OnBuffed; }

    public BoolBuffContainer InvincibleBuffer { get; set; }
    public bool Invincible { get => InvincibleBuffer.BuffedStat; }
    public Action<bool> OnInvincibleChanged { get => InvincibleBuffer.OnBuffed; }

    public BoolBuffContainer SuperArmourBuffer { get; set; }
    public bool SuperArmour { get => SuperArmourBuffer.BuffedStat; }
    public Action<bool> OnSuperArmourChanged { get => SuperArmourBuffer.OnBuffed; }

    public Action OnDamaged { get; set; }
    public Action OnDie { get; set; }
    public Action OnHealed { get; set; }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin);
    public void Die();
    public void Heal(int amount);

}