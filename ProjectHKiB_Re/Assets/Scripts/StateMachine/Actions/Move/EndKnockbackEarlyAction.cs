using UnityEngine;
[CreateAssetMenu(fileName = "EndKnockbackEarlyAction", menuName = "State Machine/Action/Move/EndKnockbackEarly")]
public class EndKnockbackEarlyAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            movable.EndKnockbackEarly();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
