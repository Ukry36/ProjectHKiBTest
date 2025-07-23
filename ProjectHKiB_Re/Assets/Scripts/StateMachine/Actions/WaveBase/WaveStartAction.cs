using UnityEngine;
[CreateAssetMenu(fileName = "WaveStartAction", menuName = "Scriptable Objects/State Machine/Action/WaveBase/WaveStart")]
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
