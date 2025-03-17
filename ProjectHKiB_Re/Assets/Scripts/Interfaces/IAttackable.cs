using System.Numerics;

public interface IAttackable
{
    public CustomVariable<float> ATK { get; set; }
    public CustomVariable<float> CriticalChanceRate { get; set; }
    public CustomVariable<float> CriticalDamageRate { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }
    public Vector3 GetAttackOrigin();
}