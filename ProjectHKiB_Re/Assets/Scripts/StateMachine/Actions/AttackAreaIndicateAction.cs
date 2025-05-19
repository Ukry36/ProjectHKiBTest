using UnityEngine;
[CreateAssetMenu(fileName = "AttackAreaIndicateAction", menuName = "Scriptable Objects/State Machine/Action/AttackAreaIndicate", order = 3)]
public class AttackAreaIndicateAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) && stateController.TryGetInterface(out IAttackAreaIndicatable areaIndicatable) && stateController.TryGetInterface(out IEntityStateController controller))
        {
            areaIndicatable.LastAttackAreaIndicatorID =
            GameManager.instance.attackAreaIndicatorManager.IndicateAttackArea
            (
                attackable.AttackDatas[attackable.AttackController.AttackNumber].attackAreaIndicatorData,
                stateController.transform,
                controller.AnimationController.LastSetAnimationQuaternion4,
                () => areaIndicatable.LastAttackAreaIndicatorID = 0
            );
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}