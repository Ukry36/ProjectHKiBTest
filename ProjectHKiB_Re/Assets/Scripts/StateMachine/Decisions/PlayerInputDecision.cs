using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Input Decision", menuName = "Scriptable Objects/State Machine/Decision/Player Input Decision", order = 4)]
public class PlayerInputDecision : StateDecisionSO
{
    [SerializeField] private EnumManager.InputType _inputType;
    public override bool Decide(StateController stateController)
    {
        return GameManager.instance.inputManager.GetInputByEnum(_inputType);
    }
}
