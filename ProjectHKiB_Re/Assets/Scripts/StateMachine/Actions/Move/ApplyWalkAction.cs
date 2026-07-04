using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class ApplyWalkAction : StateAction
    {
        public bool apply;
        public override void Act(StateController stateController)
        {
            var movable = stateController.GetInterface<IPhysics>();
            if (movable != null)
            {
                movable.IsWalking = apply;
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");

        }
    }
}