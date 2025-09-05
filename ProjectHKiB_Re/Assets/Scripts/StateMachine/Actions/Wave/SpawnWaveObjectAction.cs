using UnityEngine;
[CreateAssetMenu(fileName = "SpawnWaveObjectAction", menuName = "State Machine/Action/Wave/SpawnWaveObject")]
public class SpawnWaveObjectAction : StateActionSO
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
