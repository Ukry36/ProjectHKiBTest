using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Input Decision", menuName = "Scriptable Objects/State Machine/Decision/Player Input Decision", order = 4)]
public class PlayerInputDecision : StateDecisionSO
{
    private enum InputType
    {
        OnMove,
        OnSprint,
        OnAttack
    }
    [SerializeField] private InputType _inputType;
    public override bool Decide(StateController stateController)
    {
        return _inputType switch
        {
            InputType.OnMove => !GameManager.instance.inputManager.MoveInput.Equals(Vector2.zero),
            InputType.OnSprint => GameManager.instance.inputManager.SprintInput,
            InputType.OnAttack => GameManager.instance.inputManager.AttackInput,
            _ => false,
        };
    }
}
