using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class PlayerInputDecision : StateDecision
    {
        [SerializeField] private EnumManager.InputType _inputType;
        public override bool Decide(StateController stateController)
        {
            return GameManager.instance.inputManager.GetInputByEnum(_inputType);
        }
    }
}