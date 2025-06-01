using UnityEngine;

public interface IAttackable
{
    public int ATK { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get => AttackController.CurrentTarget; set => AttackController.CurrentTarget = value; }
}