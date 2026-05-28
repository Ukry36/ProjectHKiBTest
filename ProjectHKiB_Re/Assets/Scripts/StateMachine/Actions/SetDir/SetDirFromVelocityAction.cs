using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromVelocityAction", menuName = "State Machine/Action/SetDir/SetDirFromVelocity")]
public class SetDirFromVelocityAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IPhysics movable))
        {
            Vector2 dir = movable.ExForce * (negative ? -1 : 1);
            if (stateController.TryGetInterface(out IDirAnimatable animatable))
                animatable.SetAnimationDirection(dir);
        }

    }
}
