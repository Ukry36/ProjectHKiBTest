using UnityEngine;
[CreateAssetMenu(fileName = "WaveEventEndAction", menuName = "Scriptable Objects/State Machine/Action/WaveBase/WaveEventEnd")]
public class WaveEventEndAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
        {
            wave.WaveEventEnded();
        }
        else Debug.Log("Wave is Null");
    }
}
