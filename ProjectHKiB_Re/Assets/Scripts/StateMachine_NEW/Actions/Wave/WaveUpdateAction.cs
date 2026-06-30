using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveUpdateAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            {
                wave.CurrentWaveData.UpdateAction(stateController);
            }
            else Debug.Log("Wave is Null");
        }
    }
}