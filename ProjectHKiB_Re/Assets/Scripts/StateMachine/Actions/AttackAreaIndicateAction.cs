using UnityEngine;
[CreateAssetMenu(fileName = "AttackAreaIndicateAction", menuName = "Scriptable Objects/State Machine/Action/AttackAreaIndicate", order = 3)]
public class AttackAreaIndicateAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) && stateController.TryGetInterface(out IMovable movable))
        {
            GameManager.instance.attackAreaIndicatorManager.IndicateAttackArea
            (
                attackable.AttackDatas[attackable.AttackController.AttackNumber].attackAreaIndicatorData,
                movable.MovePoint.transform,
                stateController.animationController.LastSetAnimationQuaternion4
            );
        }

    }
}