using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "WaveEventEndedDecision", menuName = "Scriptable Objects/State Machine/Decision/WaveBase/WaveEventEndedDecision")]
public class WaveEventEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            return wave.CurrentWaveIndex >= wave.AllWavesCount;
        Debug.Log("Wave is Null");
        return false;
    }
}
