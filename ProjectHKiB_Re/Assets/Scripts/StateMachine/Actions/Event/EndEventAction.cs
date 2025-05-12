using UnityEngine;
[CreateAssetMenu(fileName = "EndEventAction", menuName = "Scriptable Objects/State Machine/Action/EndEvent")]
public class EndEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IEventController controller))
        {
            controller.EndEvent();
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
