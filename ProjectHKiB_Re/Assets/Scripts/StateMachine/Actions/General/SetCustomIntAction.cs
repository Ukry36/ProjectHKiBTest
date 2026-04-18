using UnityEngine;
[CreateAssetMenu(fileName = "SetCustomInt", menuName = "State Machine/Action/General/SetCustomInt")]
public class SetCustomIntAction : StateActionSO
{
    public string intName;
    public int value;
    public override void Act(StateController stateController)
    {
        stateController.SetIntParameter(intName, value);
    }
}
