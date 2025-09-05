using UnityEngine;
[CreateAssetMenu(fileName = "WaveClearAction", menuName = "State Machine/Action/WaveInternal/WaveClear")]
public class WaveClearAction : StateActionSO
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
