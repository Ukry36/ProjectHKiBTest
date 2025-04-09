using System.Collections.Generic;
using UnityEngine;

public class RuntimePool<T>
{
    public Dictionary<int, List<T>> pool = new();

    public bool CheckPoolAvailable(int ID)
    {
        if (!pool.ContainsKey(ID))
            Debug.LogError("ERROR: ID " + ID + " is missing!!!");
        return pool.ContainsKey(ID) && pool[ID].Count > 0;
    }

    public void EnqueuePool(int ID, T t)
    {
        //Debug.Log("ID " + ID + ", " + t + " enqueued!");
        if (!pool.ContainsKey(ID))
            pool.Add(ID, new List<T>());

        pool[ID].Add(t);
    }

    public T DequeuePool(int ID)
    {
        if (!CheckPoolAvailable(ID))
            return default;
        T value = pool[ID][0];
        pool[ID].RemoveAt(0);
        return value;
    }

    public void DeleteObjectFromPool(int ID, T t)
    {
        if (!CheckPoolAvailable(ID)) return;

        pool[ID].Remove(t);
    }
}
