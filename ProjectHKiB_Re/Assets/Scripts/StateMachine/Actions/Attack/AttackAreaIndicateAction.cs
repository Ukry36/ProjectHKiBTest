using UnityEngine;
[CreateAssetMenu(fileName = "AttackAreaIndicateAction", menuName = "State Machine/Action/Attack/AttackAreaIndicate")]
public class AttackAreaIndicateAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) && stateController.TryGetInterface(out IAttackAreaIndicatable areaIndicatable) && stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            areaIndicatable.LastAttackAreaIndicatorID =
            GameManager.instance.attackAreaIndicatorManager.IndicateAttackArea
            (
                attackable.AttackDatas[attackable.AttackController.AttackNumber].attackAreaIndicatorData,
                stateController.transform,
                animatable.AnimationController.LastSetAnimationQuaternion4,
                () => areaIndicatable.LastAttackAreaIndicatorID = 0
            );
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}