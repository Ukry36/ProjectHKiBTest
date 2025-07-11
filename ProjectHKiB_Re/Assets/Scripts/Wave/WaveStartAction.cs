using UnityEngine;
[CreateAssetMenu(fileName = "WaveStartAction", menuName = "Scriptable Objects/Wave/WaveStartAction")]
public class WaveStartAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave))
        {
            wave?.GetCurrentWaveData().StartAction(stateController);
        }
    }
}
