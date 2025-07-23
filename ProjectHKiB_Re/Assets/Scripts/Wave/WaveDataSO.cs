using System;
using UnityEngine;

[Serializable]
public class ObjectSpawnData
{
    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField] public int Count { get; private set; }
    [field: SerializeField] public bool Record { get; private set; }
}

[Serializable]
public class AndDecision
{
    [field: SerializeField] public StateDecisionSO[] StateDecisionsAnd { get; set; }
    public bool Decide(StateController stateController)
    {
        bool decide = true;
        for (int j = 0; j < StateDecisionsAnd.Length; j++)
        {
            if (!StateDecisionsAnd[j].Decide(stateController))
            {
                decide = false;
                break;
            }
        }
        return decide;
    }
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/Wave/WaveData")]
public class WaveDataSO : ScriptableObject
{
    [SerializeField] private StateActionSO[] _startActions;
    [SerializeField] private StateActionSO[] _updateActions;
    [SerializeField] private AndDecision[] _waveEndDecisionsOr;
    public int ObjectCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < ObjectSpawnDatas.Length; i++)
            {
                if (ObjectSpawnDatas[i].Record)
                    count += ObjectSpawnDatas[i].Count;
            }
            return count;
        }
    }
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
        for (int j = 0; j < _waveEndDecisionsOr.Length; j++)
        {
            if (_waveEndDecisionsOr[j].Decide(stateController))
                return true;
        }
        return false;
    }
}