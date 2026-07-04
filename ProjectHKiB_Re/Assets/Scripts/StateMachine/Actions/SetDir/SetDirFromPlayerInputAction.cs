using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetDirFromPlayerInputAction : StateAction
    {
        [SerializeField] private bool negative;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDirAnimatable animatable))
                animatable.SetAnimationDirection(negative ? GameManager.instance.inputManager.MoveInput * -1 : GameManager.instance.inputManager.MoveInput);
        }
    }
}