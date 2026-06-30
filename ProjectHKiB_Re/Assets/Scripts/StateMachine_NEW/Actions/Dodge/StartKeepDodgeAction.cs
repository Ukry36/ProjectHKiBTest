using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class StartKeepDodgeAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDodgeable dodgeable))
            {
                dodgeable.StartKeepDodge();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}