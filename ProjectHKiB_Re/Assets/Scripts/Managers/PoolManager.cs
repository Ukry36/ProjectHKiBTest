using System.Collections.Generic;
using UnityEngine;

public abstract class PoolManager<T> : MonoBehaviour
{
    public Dictionary<int, T> objects;
    public Transform defaultParent;
    public RuntimePool activeObjectSet;
    public RuntimePool inactiveObjectSet;

    public virtual void Initialize()
    {
        ResetPool();

        InitializePool();
    }

    public T GetObject(int hash) => objects[hash];

    public abstract void InitializePool();
    // you need to write down codes which instantiate gameobjects from certain datas
    //such as:

    /**********************************************************************************************
    base.CreatePool();

    for (int i = 0; i < allDatas.Length; i++)
    {
        for (int j = 0; j < allDatas[i].type.poolSize; j++)
        {
            var clone = Instantiate(prefab, this.transform);
            if (clone.TryGetComponent(out AudioPlayer audioPlayer))
            {
                AddPool(allDatas[i].ID, audioPlayer);
                audioPlayer.Initialize(allDatas[i]);
                audioPlayer.OnGameObjectDisabled += OnGameObjectDisabled;
            }
            else
            {
                Debug.LogError("ERROR: Failed to create pool(audioPlayer prefab is invalid)!!!");
            }
        }
    }
    **************************************************************************************************/


    public void AddObjectToPool(int ID, T t)
    {
        objects.Add(t.GetHashCode(), t);
        inactiveObjectSet.EnqueuePool(ID, t.GetHashCode());
    }

    public void CreatePool(int ID, int poolSize)
    {
        inactiveObjectSet.AddPool(ID, poolSize);
        activeObjectSet.AddPool(ID, poolSize);
    }

    public T ReuseObject(int ID, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        T t = default;
        if (inactiveObjectSet.CheckPoolAvailable(ID))
        {
            t = GetObject(inactiveObjectSet.DequeuePool(ID));
        }
        else if (activeObjectSet.CheckPoolAvailable(ID))
        {
            t = GetObject(activeObjectSet.DequeuePool(ID));
        }

        if (t != null)
        {
            activeObjectSet.EnqueuePool(ID, t.GetHashCode());
            InitObjectOnReuse(t, transform, rotation, attatchToTransform);
            return t;
        }

        Debug.LogError("ERROR: Failed to reuse object(no object pool for) ID: " + ID);
        return t;

    }

    public abstract void InitObjectOnReuse(T t, Transform transform, Quaternion quaternion, bool attatchToTransform);

    public void OnObjectUseEnded(int ID, int instanceID)
    {
        activeObjectSet.DeleteObjectFromPool(ID, instanceID);
        inactiveObjectSet.EnqueuePool(ID, instanceID);
    }

    public virtual void ResetPool()
    {
        objects = null;
        activeObjectSet = null;
        inactiveObjectSet = null;
    }
}