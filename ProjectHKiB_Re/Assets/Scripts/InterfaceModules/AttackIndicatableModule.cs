using UnityEngine;

public class AttackIndicatableModule : InterfaceModule, IAttackIndicatable
{
    public int LastAttackIndicatorID { get; set; }
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IAttackIndicatable>(this);
    }
    public void Initialize()
    {
        this.gameObject.SetActive(true);
    }

    public void StartIndicating(AttackAreaIndicatorData indicatorData, Transform transform, Quaternion quaternion)
    {
        LastAttackIndicatorID =
            GameManager.instance.attackAreaIndicatorManager.IndicateAttackArea
            (
                indicatorData,
                transform,
                quaternion,
                () => LastAttackIndicatorID = 0
            );
    }

    public void EndIndicating()
    {
        if (LastAttackIndicatorID != 0)
            GameManager.instance.attackAreaIndicatorManager.StopIndicating(LastAttackIndicatorID);
    }


}