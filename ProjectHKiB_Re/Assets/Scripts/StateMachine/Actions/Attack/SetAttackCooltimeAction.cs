using UnityEngine;
[CreateAssetMenu(fileName = "SetAttackCooltimeAction", menuName = "State Machine/Action/Attack/SetAttackCooltime")]
public class SetAttackCooltimeAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            attackable.StartAttackCooltime();
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
