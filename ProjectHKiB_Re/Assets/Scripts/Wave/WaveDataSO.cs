using System;
using UnityEngine;

[Serializable]
public class ObjectSpawnData
{
    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField] public int Count { get; private set; }
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/Wave/WaveData")]
public class WaveDataSO : ScriptableObject
{
    [SerializeField] private StateActionSO[] _startActions;
    [SerializeField] private StateActionSO[] _updateActions;
    [SerializeField] private StateDecisionSO[] _waveEndDecisions;

    [field: SerializeField] public ObjectSpawnData[] ObjectSpawnDatas { get; private set; }

    public void StartAction(StateController stateController)
    {
        for (int i = 0; i < _startActions.Length; i++)
        {
            _startActions[i].Act(stateController);
        }
    }
    public void UpdateAction(StateController stateController)
    {
        for (int i = 0; i < _updateActions.Length; i++)
        {
            _updateActions[i].Act(stateController);
        }
    }
    public bool WaveEndDecision(StateController stateController)
    {
        bool canEnd = true;
        for (int j = 0; j < _waveEndDecisions.Length; j++)
        {
            if (!_waveEndDecisions[j].Decide(stateController))
            {
                canEnd = false;
                break;
            }
        }
        return canEnd;
    }
}