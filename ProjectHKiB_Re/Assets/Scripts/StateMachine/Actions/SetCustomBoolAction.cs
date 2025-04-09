using UnityEngine;
[CreateAssetMenu(fileName = "SetCustomBool", menuName = "Scriptable Objects/State Machine/Action/SetCustomBool", order = 3)]
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
