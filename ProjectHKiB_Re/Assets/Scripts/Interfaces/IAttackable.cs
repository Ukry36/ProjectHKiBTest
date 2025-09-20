using System;
using UnityEngine;

public interface IAttackable
{
    public int BaseATK { get; set; }
    public FloatBuffContainer ATKBuffer { get; set; }
    public int ATK { get => (int)ATKBuffer.BuffedStat; }
    public EventHandler<float> OnATKChanged { get => ATKBuffer.OnBuffed; }

    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
}