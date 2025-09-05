using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "WaveEventEndedDecision", menuName = "State Machine/Decision/WaveInternal/WaveEventEndedDecision")]
public class WaveEventEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave))
            return wave.CurrentWaveIndex >= wave.AllWavesCount;
        Debug.Log("Wave is Null");
        return false;
    }
}
