using UnityEngine;
[CreateAssetMenu(fileName = "AttackAreaIndicateAction", menuName = "State Machine/Action/Attack/AttackAreaIndicate")]
public class AttackAreaIndicateAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) && stateController.TryGetInterface(out IAttackIndicatable areaIndicatable) && stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            areaIndicatable.LastAttackIndicatorID =
            GameManager.instance.attackAreaIndicatorManager.IndicateAttackArea
            (
                attackable.AttackDatas[attackable.AttackNumber].attackAreaIndicatorData,
                stateController.transform,
                animatable.LastSetAnimationQuaternion4,
                () => areaIndicatable.LastAttackIndicatorID = 0
            );
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}