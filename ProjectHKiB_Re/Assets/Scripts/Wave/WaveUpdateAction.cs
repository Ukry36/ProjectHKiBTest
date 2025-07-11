using UnityEngine;
[CreateAssetMenu(fileName = "WaveUpdateAction", menuName = "Scriptable Objects/Wave/WaveUpdateAction")]
public class WaveUpdateAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave))
        {
            wave?.GetCurrentWaveData().UpdateAction(stateController);
        }
    }
}
