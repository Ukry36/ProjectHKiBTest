using UnityEngine;
[CreateAssetMenu(fileName = "SetCustomBool", menuName = "State Machine/Action/General/SetCustomBool")]
public class SetCustomBoolAction : StateActionSO
{
    public string boolName;
    public bool value;
    public override void Act(StateController stateController)
    {
        if (value)
            stateController.SetBoolParameterTrue(boolName);
        else
            stateController.SetBoolParameterFalse(boolName);
    }
}
