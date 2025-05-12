using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Input Decision", menuName = "Scriptable Objects/State Machine/Decision/Player Input Decision", order = 4)]
public class PlayerInputDecision : StateDecisionSO
{
    private enum InputType
    {
        OnMove,
        OnSprint,
        OnAttack,
        OnDodge,
        HasDInput,
        HasLInput,
        HasRInput,
        HasUInput,
    }
    [SerializeField] private InputType _inputType;
    public override bool Decide(StateController stateController)
    {
        return _inputType switch
        {
            InputType.OnMove => !GameManager.instance.inputManager.MoveInput.Equals(Vector2.zero),
            InputType.OnSprint => GameManager.instance.inputManager.SprintInput,
            InputType.OnAttack => GameManager.instance.inputManager.AttackInput,
            InputType.OnDodge => GameManager.instance.inputManager.DodgeInput,
            InputType.HasDInput => GameManager.instance.inputManager.MoveInput.y < 0,
            InputType.HasLInput => GameManager.instance.inputManager.MoveInput.x < 0,
            InputType.HasRInput => GameManager.instance.inputManager.MoveInput.x > 0,
            InputType.HasUInput => GameManager.instance.inputManager.MoveInput.y > 0,
            _ => false,
        };
    }
}
