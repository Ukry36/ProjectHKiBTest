using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WaveEventEndAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave))
            {
                wave.WaveEventEnded();
            }
            else Debug.Log("Wave is Null");
        }
    }
}