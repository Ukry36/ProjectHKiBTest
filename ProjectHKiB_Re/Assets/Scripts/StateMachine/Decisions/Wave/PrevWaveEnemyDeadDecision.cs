using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "PrevWaveEnemyDeadDecision", menuName = "Scriptable Objects/State Machine/Decision/Wave/PrevWaveEnemyDead")]
public class PrevWaveEnemyDeadDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            return wave.PrevWaveEnemyDead;
        Debug.Log("Wave is Null");
        return false;
    }
}
