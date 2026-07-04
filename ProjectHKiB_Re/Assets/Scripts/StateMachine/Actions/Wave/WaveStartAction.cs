using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveStartAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            {
                wave.WaveStarted();
                wave.CurrentWaveData.StartAction(stateController);
            }
            else Debug.Log("Wave is Null");
        }
    }
}