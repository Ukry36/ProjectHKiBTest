using UnityEngine;

public interface IAttackIndicatable : IInitializable
{
    public int LastAttackIndicatorID { get; set; }
    public void StartIndicating(AttackAreaIndicatorData indicatorData, Transform transform, Quaternion quaternion);
    public void EndIndicating();
}