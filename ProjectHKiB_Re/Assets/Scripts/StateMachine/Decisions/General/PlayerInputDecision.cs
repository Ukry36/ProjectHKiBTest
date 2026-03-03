using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Input Decision", menuName = "State Machine/Decision/General/Player Input Decision")]
public class PlayerInputDecision : StateDecisionSO
{
    [SerializeField] private EnumManager.InputType _inputType;
    public override bool Decide(StateController stateController)
    {
        bool result = GameManager.instance.inputManager.GetInputByEnum(_inputType);
        Debug.Log($"[PlayerInputDecision] Type: {_inputType}, Result: {result}");
        return result;
    }

    public EnumManager.InputType InputType => _inputType;

    public void SetInputType(EnumManager.InputType inputType)
    {
        _inputType = inputType;
    }
}