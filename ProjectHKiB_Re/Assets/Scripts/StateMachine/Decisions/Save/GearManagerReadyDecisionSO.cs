using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class GearManagerReadyDecisionSO : StateDecision
    {
        public override bool Decide(StateController controller)
        {
            var module = controller.GetInterface<SaveModule>();
            if (module == null) return false;

            return module.IsGearManagerReady;
        }
    }
}