using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Input Decision", menuName = "State Machine/Decision/General/Player Input Decision")]
public class PlayerInputDecision : StateDecisionSO
{
    [SerializeField] private EnumManager.InputType _inputType;
    public override bool Decide(StateController stateController)
    {
        return GameManager.instance.inputManager.GetInputByEnum(_inputType);
    }
}
