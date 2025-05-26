using UnityEngine;
[CreateAssetMenu(fileName = "EndEventAction", menuName = "Scriptable Objects/State Machine/Action/Event/EndEvent")]
public class EndEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IEvent @event))
        {
            @event.EndEvent();
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
