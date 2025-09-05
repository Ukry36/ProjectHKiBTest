using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "CustomBoolDecision", menuName = "State Machine/Decision/General/CustomBoolDecision")]
public class CustomBoolDecision : StateDecisionSO
{
    [SerializeField] private string boolName;
    public override bool Decide(StateController stateController)
    {
        return stateController.GetBoolParameter(boolName);
    }
}
