using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SpawnWaveObjectAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IWaveEventable wave) && wave.CurrentWaveData)
            {
                wave.SpawnCurrentWaveEnemies();
            }
            else Debug.Log("Wave is Null");
        }
    }
}