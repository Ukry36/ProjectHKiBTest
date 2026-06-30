using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveClearAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            {
                wave.WaveCleared();
            }
            else Debug.Log("Wave is Null");
        }
    }
}