using System.Linq;
using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetEventFlagAction : StateAction
    {
        public EventFlagSO flag;
        public int flagValue;
        public override void Act(StateController stateController)
        {
            GameManager.instance.eventManager.SetEventFlag(flag, flagValue);
        }
    }
}