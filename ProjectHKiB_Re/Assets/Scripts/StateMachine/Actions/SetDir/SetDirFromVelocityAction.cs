using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromVelocityAction", menuName = "Scriptable Objects/State Machine/Action/SetDir/SetDirFromVelocity")]
public class SetDirFromVelocityAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            Vector2 dir = movable.ExForce.GetForce * (negative ? -1 : 1);
            if (stateController.TryGetInterface(out IDirAnimatable animatable))
                animatable.AnimationController.SetAnimationDirection(dir);
        }

    }
}
