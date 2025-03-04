using System.Collections.Generic;
using UnityEngine;

public abstract class PoolManager<T> : MonoBehaviour
{
    public GameObject prefab;
    public Dictionary<int, T> objectPool;
    public Transform defaultParent;
    public RuntimePool<int> activeObjectSet;
    public RuntimePool<int> inactiveObjectSet;

    public virtual void Initialize()
    {
        ResetPool();

        CreatePool();
    }

    public T GetObject(int hash) => objectPool[hash];

    public virtual void CreatePool()
    {
        objectPool = new();
        inactiveObjectSet = new();
        activeObjectSet = new();

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
    }

    public void AddPool(int ID, T t)
    {
        objectPool.Add(t.GetHashCode(), t);
        inactiveObjectSet.EnqueuePool(ID, t.GetHashCode());
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
            SetObjectOnReuse(t, transform, rotation, attatchToTransform);
            return t;
        }

        Debug.LogError("ERROR: Failed to reuse object(no object pool for) ID: " + ID);
        return t;

    }

    public abstract void SetObjectOnReuse(T t, Transform transform, Quaternion quaternion, bool attatchToTransform);

    public void OnGameObjectDisabled(int ID, int hash)
    {
        activeObjectSet.DeleteObjectFromPool(ID, hash);
        inactiveObjectSet.EnqueuePool(ID, hash);
    }

    public virtual void ResetPool()
    {
        objectPool = null;
        activeObjectSet = null;
        inactiveObjectSet = null;
    }
}