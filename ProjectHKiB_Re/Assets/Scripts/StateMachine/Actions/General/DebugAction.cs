using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DebugAction : StateAction
    {
        public string str;
        public override void Act(StateController stateController)
        {
            Debug.Log(stateController.name + "/" + stateController.CurrentState.name + ": " + str);
        }
    }
}