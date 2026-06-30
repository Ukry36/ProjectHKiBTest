using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetCustomIntAction : StateAction
    {
        public string intName;
        public int value;
        public override void Act(StateController stateController)
        {
            stateController.SetIntParameter(intName, value);
        }
    }
}