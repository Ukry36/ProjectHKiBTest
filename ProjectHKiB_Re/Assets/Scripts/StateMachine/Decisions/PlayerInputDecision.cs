using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Input Decision", menuName = "Scriptable Objects/State Machine/Decision/Player Input Decision", order = 4)]
public class PlayerInputDecision : StateDecisionSO
{
    private enum InputType
    {
        OnMove,
        OnSprint,
    }
    [SerializeField] private InputType _inputType;
    [SerializeField] private bool _bool;
    public override bool Decide(StateController stateController)
    {
        return _inputType switch
        {
            InputType.OnMove => _bool ^ InputManager.instance.MoveInput.Equals(Vector2.zero),
            InputType.OnSprint => !_bool ^ InputManager.instance.SprintInput,
            _ => false,
        };
    }
}
