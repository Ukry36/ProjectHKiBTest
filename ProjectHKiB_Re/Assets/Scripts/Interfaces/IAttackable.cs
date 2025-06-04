using UnityEngine;

public interface IAttackable
{
    public int BaseATK { get; set; }
    public int ATK { get => (int)AttackController.ATKBuffer.GetBuffedStat(BaseATK); }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
}