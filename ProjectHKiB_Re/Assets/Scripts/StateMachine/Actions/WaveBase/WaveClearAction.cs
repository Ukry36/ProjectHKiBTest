using UnityEngine;
[CreateAssetMenu(fileName = "WaveClearAction", menuName = "Scriptable Objects/State Machine/Action/WaveBase/WaveClear")]
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
