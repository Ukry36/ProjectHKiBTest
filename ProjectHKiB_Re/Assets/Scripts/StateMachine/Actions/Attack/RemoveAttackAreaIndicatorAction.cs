using UnityEngine;
[CreateAssetMenu(fileName = "RemoveAttackAreaIndicatorAction", menuName = "State Machine/Action/Attack/RemoveAttackAreaIndicator")]
public class RemoveAttackAreaIndicatorAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackIndicatable areaIndicatable))
        {
            if (areaIndicatable.LastAttackIndicatorID != 0)
                GameManager.instance.attackAreaIndicatorManager.StopIndicating(areaIndicatable.LastAttackIndicatorID);
        }
    }
}