using UnityEngine;

[CreateAssetMenu(
    fileName = "WalkByLastSetAnimationDir",
    menuName = "State Machine/Action/Move/WalkByLastSetAnimationDirAction")]
public class WalkByLastSetAnimationDirAction : StateActionSO
{
    [SerializeField] private bool _negate;

    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IPhysics phys) &&
            stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            Vector2 dir = animatable.LastSetAnimationDir8;

            if (_negate)
                dir *= -1f;

            phys.WalkingDir = dir;
            phys.IsWalking = true;
        }
        else
        {
            Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}