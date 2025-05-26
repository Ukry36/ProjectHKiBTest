using UnityEngine;
[CreateAssetMenu(fileName = "RemoveAttackAreaIndicatorAction", menuName = "Scriptable Objects/State Machine/Action/Attack/RemoveAttackAreaIndicator")]
public class RemoveAttackAreaIndicatorAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackAreaIndicatable areaIndicatable))
        {
            if (areaIndicatable.LastAttackAreaIndicatorID != 0)
                GameManager.instance.attackAreaIndicatorManager.StopIndicating(areaIndicatable.LastAttackAreaIndicatorID);
        }
    }
}