using UnityEngine;
[CreateAssetMenu(fileName = "Debug Action", menuName = "State Machine/Action/General/Debug")]
public class DebugAction : StateActionSO
{
    public string str;
    public override void Act(StateController stateController)
    {
        Debug.Log(stateController.name + "/" + stateController.CurrentState.name + ": " + str);
    }
}
