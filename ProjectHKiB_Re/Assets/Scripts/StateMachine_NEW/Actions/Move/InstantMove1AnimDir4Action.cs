using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class InstantMove1AnimDir4Action : StateAction
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
}