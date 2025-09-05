using UnityEngine;
[CreateAssetMenu(fileName = "WaveEventStartAction", menuName = "State Machine/Action/WaveInternal/WaveEventStart")]
public class WaveEventStartAction : StateActionSO
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
