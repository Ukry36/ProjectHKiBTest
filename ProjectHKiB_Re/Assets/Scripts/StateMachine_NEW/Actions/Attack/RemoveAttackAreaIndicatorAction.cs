using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class RemoveAttackAreaIndicatorAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackIndicatable areaIndicatable))
            {
                areaIndicatable.EndIndicating();
            }
        }
    }
}