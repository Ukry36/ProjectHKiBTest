using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class InstantMove1Dir4Action : StateAction
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
}