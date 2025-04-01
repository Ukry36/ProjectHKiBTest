using System.Collections.Generic;
using UnityEngine;

public class RuntimePool<T>
{
    public Dictionary<int, List<T>> pool;

    public bool CheckPoolAvailable(int ID) => pool.ContainsKey(ID) && pool[ID].Count > 0;

    public void EnqueuePool(int ID, T t)
    {
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
