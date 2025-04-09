using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
public class EnemyManager : PoolManager<Enemy>
{
    [SerializeField] private EnemyDataSO[] allDatas;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void CreatePool()
    {
        base.CreatePool();
        for (int i = 0; i < allDatas.Length; i++)
        {
            for (int j = 0; j < allDatas[i].PoolSize; j++)
            {
                var clone = Instantiate(prefab, this.transform);
                if (clone.TryGetComponent(out Enemy enemy))
                {
                    AddPool(allDatas[i].GetInstanceID(), enemy);
                    enemy.InitializeFromPool(allDatas[i]);
                    enemy.OnGameObjectDisabled += OnGameObjectDisabled;
                }
                else
                {
                    Debug.LogError("ERROR: Failed to create pool(Enemy prefab is invalid)!!!");
                }
            }
        }
    }

    public override void SetObjectOnReuse(Enemy enemy, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        enemy.gameObject.SetActive(true);
        enemy.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) enemy.transform.parent = transform;
    }

    public override void ResetPool()
    {
        int[] keys = objectPool.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
            Destroy(objectPool[keys[i]].gameObject);
        base.ResetPool();
    }
}