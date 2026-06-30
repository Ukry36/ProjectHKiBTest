using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CurrentWaveEnemyDeadDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
                return wave.CurrentWaveEnemyDead;
            Debug.Log("Wave is Null");
            return false;
        }
    }
}