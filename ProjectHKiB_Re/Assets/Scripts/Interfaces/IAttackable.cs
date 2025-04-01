using UnityEngine;

public interface IAttackable
{
    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }
    public Vector3 GetAttackOrigin();
}