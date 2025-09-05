using UnityEngine;
[CreateAssetMenu(fileName = "WaveEventEndAction", menuName = "State Machine/Action/WaveInternal/WaveEventEnd")]
public class WaveEventEndAction : StateActionSO
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
