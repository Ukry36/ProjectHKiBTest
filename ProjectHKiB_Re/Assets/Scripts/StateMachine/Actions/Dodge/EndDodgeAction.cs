using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class EndDodgeAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDodgeable dodgeable))
            {
                dodgeable.EndDodge();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}