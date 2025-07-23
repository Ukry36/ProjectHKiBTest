using UnityEngine;
[CreateAssetMenu(fileName = "WaveEventStartAction", menuName = "Scriptable Objects/State Machine/Action/WaveBase/WaveEventStart")]
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
