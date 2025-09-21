using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "AnimationEndedDecision", menuName = "State Machine/Decision/General/AnimationEndedDecision", order = 4)]
public class AnimationEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDirAnimatable animatable))
            //if (stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
            //    Debug.Log(stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            return animatable.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
        return false;
    }
}
