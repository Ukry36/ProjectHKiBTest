using UnityEngine;
[CreateAssetMenu(fileName = "InstantMove1AnimDir4Action", menuName = "State Machine/Action/Move/InstantMove1AnimDir4")]
public class InstantMove1AnimDir4Action : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IPhysics phys) && stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            phys.LogicalTeleport(phys.Position + (Vector3)animatable.LastSetAnimationDir4);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
