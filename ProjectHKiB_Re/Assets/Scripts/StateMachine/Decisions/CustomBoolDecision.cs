using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "CustomBoolDecision", menuName = "Scriptable Objects/State Machine/Decision/CustomBoolDecision", order = 4)]
public class CustomBoolDecision : StateDecisionSO
{
    [SerializeField] private string boolName;
    public override bool Decide(StateController stateController)
    {
        return stateController.GetBoolParameter(boolName);
    }
}
