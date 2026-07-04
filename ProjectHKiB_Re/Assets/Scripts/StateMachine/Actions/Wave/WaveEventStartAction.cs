using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveEventStartAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            {
                wave.WaveEventStarted();
            }
            else Debug.Log("Wave is Null");
        }
    }
}