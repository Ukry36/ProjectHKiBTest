using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class IncrementCustomIntAction : StateAction
    {
        public string intName;
        public int value;
        public override void Act(StateController stateController)
        {
            stateController.IncrementIntParameter(intName, value);
        }
    }
}