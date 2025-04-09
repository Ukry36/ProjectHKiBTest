using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "AnimationEndedDecision", menuName = "Scriptable Objects/State Machine/Decision/AnimationEndedDecision", order = 4)]
public class AnimationEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        return stateController.animationEndTrigger;
    }
}
