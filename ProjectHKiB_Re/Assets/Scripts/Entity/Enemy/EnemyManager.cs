using System.Linq;
using UnityEngine;
public class EnemyManager : PoolManager<Enemy>
{
    public GameObject prefab;
    [SerializeField] private EnemyDataSO[] allDatas;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void InitializePool()
    {
        objects = new();
        inactiveObjectSet = new(allDatas.Length);
        activeObjectSet = new(allDatas.Length);
        for (int i = 0; i < allDatas.Length; i++)
        {
            for (int j = 0; j < allDatas[i].PoolSize; j++)
            {
                var clone = Instantiate(prefab, this.transform);
                if (clone.TryGetComponent(out Enemy enemy))
                {
                    AddObjectToPool(allDatas[i].GetInstanceID(), enemy);
                    enemy.InitializeFromPool(allDatas[i]);
                    enemy.OnGameObjectDisabled += OnObjectUseEnded;
                }
                else
                {
                    Debug.LogError("ERROR: Failed to create pool(Enemy prefab is invalid)!!!");
                }
            }
        }
    }

    public override void InitObjectOnReuse(Enemy enemy, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        enemy.gameObject.SetActive(true);
        enemy.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) enemy.transform.parent = transform;
    }

    public override void ResetPool()
    {
        int[] keys = objects.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
            Destroy(objects[keys[i]].gameObject);
        base.ResetPool();
    }
}