using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "WaveEndedDecision", menuName = "State Machine/Decision/WaveInternal/WaveEndedDecision")]
public class WaveEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            return wave.CurrentWaveData.WaveEndDecision(stateController) || wave.ClearTrigger;
        Debug.Log("Wave is Null");
        return false;
    }
}
