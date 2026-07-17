using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[Serializable]
public class RuntimePool
{
    // ID of prefab, ID of instances
    public SerializedDictionary<int, List<int>> pool;

    public RuntimePool(int count)
    {
        pool = new(count);
    }

    public bool CheckPoolAvailable(int ID)
    {
        if (!pool.ContainsKey(ID))
        {
            Debug.LogError("ERROR: Pool of ID " + ID + " is missing!!!");
            return false;
        }
        else return pool[ID].Count > 0;
    }

    public void AddPool(int ID, int poolSize)
    {
        if (!pool.ContainsKey(ID))
        {
            pool.Add(ID, new(poolSize));
            //Debug.Log("added " + ID);
        }
    }

    public void EnqueuePool(int ID, int instanceID)
    {
        //Debug.Log("ID " + ID + ", " + t + " enqueued!");
        if (!pool.ContainsKey(ID))
            Debug.LogError("ERROR: Pool of ID " + ID + " is missing!!!");

        pool[ID].Add(instanceID);
    }

    public int DequeuePool(int ID)
    {
        if (!CheckPoolAvailable(ID))
            return default;
        int value = pool[ID][0];
        pool[ID].RemoveAt(0);
        return value;
    }

    public bool DeleteObjectFromPool(int ID, int instanceID)
    {
        if (!CheckPoolAvailable(ID)) return false;

        pool[ID].Remove(instanceID);
        return true;
    }
}
