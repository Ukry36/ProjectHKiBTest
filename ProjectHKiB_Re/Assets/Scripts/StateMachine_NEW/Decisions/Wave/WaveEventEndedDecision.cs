using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveEventEndedDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave))
                return wave.CurrentWaveIndex >= wave.AllWavesCount;
            Debug.Log("Wave is Null");
            return false;
        }
    }
}