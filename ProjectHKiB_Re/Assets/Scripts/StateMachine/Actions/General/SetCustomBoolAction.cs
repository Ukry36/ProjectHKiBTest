using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetCustomBoolAction : StateAction
    {
        public string boolName;
        public bool value;
        public override void Act(StateController stateController)
        {
            if (value)
                stateController.SetBoolParameterTrue(boolName);
            else
                stateController.SetBoolParameterFalse(boolName);
        }
    }
}