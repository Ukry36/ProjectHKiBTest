using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PoolManager<T> : MonoBehaviour
{
    public Dictionary<int, T> objects;
    public Transform defaultParent;
    public RuntimePool activeObjectSet;
    public RuntimePool inactiveObjectSet;
    public Action<int, int> OnObjectUseAction;
    public Action<int, int> OnObjectUseEndedAction;

    [SerializeField] private Transform _oneshotTransform;

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
        int instanceID = 0;
        if (inactiveObjectSet.CheckPoolAvailable(ID))
        {
            instanceID = inactiveObjectSet.DequeuePool(ID);
            t = GetObject(instanceID);
        }
        else if (activeObjectSet.CheckPoolAvailable(ID))
        {
            instanceID = activeObjectSet.DequeuePool(ID);
            OnObjectUseEndedAction?.Invoke(ID, instanceID);
            t = GetObject(instanceID);
        }

        if (t != null)
        {
            OnObjectUseAction?.Invoke(ID, instanceID);
            activeObjectSet.EnqueuePool(ID, instanceID);
            InitObjectOnReuse(t, transform, rotation, attatchToTransform);
            return t;
        }

        Debug.LogError("ERROR: Failed to reuse object(no object pool for) ID: " + ID);
        return t;
    }

    public T ReuseObjectOneShot(int ID, Vector3 pos, Quaternion rotation)
    {
        _oneshotTransform.position = pos;
        return ReuseObject(ID, _oneshotTransform, rotation, false);
    }

    public abstract void InitObjectOnReuse(T t, Transform transform, Quaternion quaternion, bool attatchToTransform);

    public virtual void OnObjectUseEnded(int ID, int instanceID)
    {
        activeObjectSet.DeleteObjectFromPool(ID, instanceID);
        inactiveObjectSet.EnqueuePool(ID, instanceID);
        OnObjectUseEndedAction?.Invoke(ID, instanceID);
    }

    public virtual void ResetPool()
    {
        objects = null;
        activeObjectSet = null;
        inactiveObjectSet = null;
    }
}