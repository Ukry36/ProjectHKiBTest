using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class LoadReadSucceededDecisionSO : StateDecision
    {
        public override bool Decide(StateController controller)
        {
            var module = controller.GetInterface<SaveModule>();
            if (module == null) return false;

            // ReadSaveFile() 실패 시 LoadedData = null 로 세팅되어 있음
            return module.LoadedData != null;
        }
    }
}