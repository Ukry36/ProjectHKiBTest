using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "WaveEndedDecision", menuName = "Scriptable Objects/State Machine/Decision/WaveBase/WaveEndedDecision")]
public class WaveEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            return wave.CurrentWaveData.WaveEndDecision(stateController);
        Debug.Log("Wave is Null");
        return false;
    }
}
