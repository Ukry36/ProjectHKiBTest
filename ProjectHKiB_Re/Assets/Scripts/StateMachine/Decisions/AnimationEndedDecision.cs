using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "AnimationEndedDecision", menuName = "Scriptable Objects/State Machine/Decision/AnimationEndedDecision", order = 4)]
public class AnimationEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IEntityStateController controller))
            //if (stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
            //    Debug.Log(stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            return controller.AnimationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
        return false;
    }
}
