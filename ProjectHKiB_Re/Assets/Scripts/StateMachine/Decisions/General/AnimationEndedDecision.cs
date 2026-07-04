namespace StateMachine
{
    [System.Serializable]
    public class AnimationEndedDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDirAnimatable animatable))
                //if (stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
                //    Debug.Log(stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                return animatable.AnimationPlayer.IsFirstLoopEnded;
            return false;
        }
    }
}