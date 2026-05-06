using UnityEngine;
[CreateAssetMenu(fileName = "WalkByLastSetAnimationDir", menuName = "State Machine/Action/Move/WalkByLastSetAnimationDirAction")]
public class WalkByLastSetAnimationDirAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            movable.IsWalking = true;
            movable.WalkingDir = animatable.LastSetAnimationDir8;
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}


