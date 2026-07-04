using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class PrevWaveEnemyDeadDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
                return wave.PrevWaveEnemyDead;
            Debug.Log("Wave is Null");
            return false;
        }
    }
}