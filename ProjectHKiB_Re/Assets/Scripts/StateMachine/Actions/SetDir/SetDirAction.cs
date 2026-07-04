using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetDirAction : StateAction
    {
        [SerializeField] private EnumManager.AnimDir animDir;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDirAnimatable animatable))
            {
                animatable.SetAnimationDirection(animDir);
            }
        }
    }
}