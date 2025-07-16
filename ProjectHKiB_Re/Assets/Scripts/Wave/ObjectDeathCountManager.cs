using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ObjectDeathCounter
{
    public int Capacity { get; set; }
    public List<int> FilterIDs { get; set; }
    private readonly List<int> _aliveEnemyInstIDs;
    private readonly List<int> _deadEnemyInstIDs;
    public int AliveObjectCount { get => _aliveEnemyInstIDs.Count; }
    public int DeadObjectCount { get => _deadEnemyInstIDs.Count; }
    public bool IsFull { get => AliveObjectCount + DeadObjectCount >= Capacity; }
    public bool IsDead { get => Capacity == DeadObjectCount; }
    public ObjectDeathCounter(int capacity, List<GameObject> filterPrefabs = null)
    {
        Capacity = capacity;
        _aliveEnemyInstIDs = new(capacity);
        _deadEnemyInstIDs = new(capacity);
        SetFilterIDs(filterPrefabs);
    }
    public void RecordDeath(int ID, int instanceID)
    {
        if (FilterIDs == null || FilterIDs.Contains(ID))
            if (_aliveEnemyInstIDs.Contains(instanceID))
            {
                _aliveEnemyInstIDs.Remove(instanceID);
                _deadEnemyInstIDs.Add(instanceID);
            }
    }

    public void RecordSpawn(int ID, int instanceID)
    {
        if (FilterIDs == null || FilterIDs.Contains(ID))
            if (!IsFull)
                _aliveEnemyInstIDs.Add(instanceID);
    }

    public void SetFilterIDs(List<GameObject> filterPrefabs)
    {
        if (filterPrefabs == null) return;
        for (int i = 0; i < filterPrefabs.Count; i++)
        {
            FilterIDs.Add(filterPrefabs[i].GetInstanceID());
        }
    }
}

public class ObjectDeathCountManager : MonoBehaviour
{
    private readonly Dictionary<int, ObjectDeathCounter> _currentCounters = new();

    public void Start()
    {
        GameManager.instance.objectSpawnManager.OnObjectUseAction += RecordSpawn;
        GameManager.instance.objectSpawnManager.OnObjectUseEndedAction += RecordDeath;
    }

    public void OnDestroy()
    {
        GameManager.instance.objectSpawnManager.OnObjectUseAction -= RecordSpawn;
        GameManager.instance.objectSpawnManager.OnObjectUseEndedAction -= RecordDeath;
    }
    public ObjectDeathCounter AddCounter(int capacity, List<GameObject> filterPrefabs = null)
    {
        ObjectDeathCounter objectDeathCounter = new(capacity, filterPrefabs);
        _currentCounters.Add(objectDeathCounter.GetHashCode(), objectDeathCounter);
        return objectDeathCounter;
    }

    public void RemoveCounter(int ID)
    {
        _currentCounters.Remove(ID);
    }

    public bool CheckCounterFull(int ID)
    {
        if (!_currentCounters.ContainsKey(ID)) return true;
        return _currentCounters[ID].IsFull;
    }

    public ObjectDeathCounter GetCounter(int ID)
    {
        if (!_currentCounters.ContainsKey(ID)) return null;
        return _currentCounters[ID];
    }

    public void RecordDeath(int ID, int instanceID)
    {
        List<ObjectDeathCounter> counters = _currentCounters.Values.ToList();
        for (int i = 0; i < counters.Count; i++)
        {
            counters[i].RecordDeath(ID, instanceID);
        }
    }

    public void RecordSpawn(int ID, int instanceID)
    {
        List<ObjectDeathCounter> counters = _currentCounters.Values.ToList();
        for (int i = 0; i < counters.Count; i++)
        {
            counters[i].RecordSpawn(ID, instanceID);
        }
    }
}