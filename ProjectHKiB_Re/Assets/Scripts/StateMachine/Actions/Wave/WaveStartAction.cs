using UnityEngine;
[CreateAssetMenu(fileName = "WaveStartAction", menuName = "State Machine/Action/WaveInternal/WaveStart")]
public class WaveStartAction : StateActionSO
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
