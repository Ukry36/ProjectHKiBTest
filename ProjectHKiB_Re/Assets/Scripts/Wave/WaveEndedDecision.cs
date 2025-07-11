using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "WaveEndedDecision", menuName = "Scriptable Objects/Wave/WaveEndedDecision")]
public class WaveEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave != null)
            return wave.GetCurrentWaveData().WaveEndDecision(stateController);
        Debug.Log("Wave is Null");
        return false;
    }
}
