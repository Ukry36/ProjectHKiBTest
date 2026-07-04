using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveEndedDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
                return wave.CurrentWaveData.WaveEndDecision(stateController) || wave.ClearTrigger;
            Debug.Log("Wave is Null");
            return false;
        }
    }
}