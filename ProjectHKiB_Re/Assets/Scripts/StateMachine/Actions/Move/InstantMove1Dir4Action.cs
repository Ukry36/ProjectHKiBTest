using UnityEngine;
[CreateAssetMenu(fileName = "InstantMove1AnimDir4Action", menuName = "State Machine/Action/Move/InstantMove1AnimDir4")]
public class InstantMove1Dir4Action : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IPhysics phys))
        {
            phys.LogicalTeleport(phys.Position + phys.LastSetDir);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
