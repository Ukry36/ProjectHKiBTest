public interface IAttackable : ITargetable
{
    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
}