using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CustomBoolDecision : StateDecision
    {
        [SerializeField] private string boolName;
        public override bool Decide(StateController stateController)
        {
            return stateController.GetBoolParameter(boolName);
        }
    }
}