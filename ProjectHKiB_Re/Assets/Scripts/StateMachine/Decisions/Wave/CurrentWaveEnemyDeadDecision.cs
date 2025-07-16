using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "CurrentWaveEnemyDeadDecision", menuName = "Scriptable Objects/State Machine/Decision/Wave/CurrentWaveEnemyDead")]
public class CurrentWaveEnemyDeadDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            return wave.CurrentWaveEnemyDead;
        Debug.Log("Wave is Null");
        return false;
    }
}
