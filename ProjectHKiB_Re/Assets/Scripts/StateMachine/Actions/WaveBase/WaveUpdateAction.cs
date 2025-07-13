using UnityEngine;
[CreateAssetMenu(fileName = "WaveUpdateAction", menuName = "Scriptable Objects/State Machine/Action/WaveBase/WaveUpdate")]
public class WaveUpdateAction : StateActionSO
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
