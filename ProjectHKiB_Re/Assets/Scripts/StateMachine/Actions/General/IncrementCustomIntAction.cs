using UnityEngine;
[CreateAssetMenu(fileName = "IncrementCustomInt", menuName = "State Machine/Action/General/IncrementCustomInt")]
public class IncrementCustomIntAction : StateActionSO
{
    public string intName;
    public int value;
    public override void Act(StateController stateController)
    {
        stateController.IncrementIntParameter(intName, value);
    }
}
