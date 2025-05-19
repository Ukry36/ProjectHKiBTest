using UnityEngine;
[CreateAssetMenu(fileName = "EndKnockbackEarlyAction", menuName = "Scriptable Objects/State Machine/Action/EndKnockbackEarly", order = 3)]
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
