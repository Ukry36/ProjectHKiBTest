using System.Linq;
using UnityEngine;
public class ObjectSpawnManager : PoolManager<GameObject>
{
    public Transform emptyTransform;
    [SerializeField] private GameObject[] allDatas;

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
            if (allDatas[i].TryGetComponent(out IPoolable poolable))
            {
                for (int j = 0; j < poolable.PoolSize; j++)
                {
                    var clone = Instantiate(allDatas[i], this.transform);
                    AddObjectToPool(allDatas[i].GetInstanceID(), clone);
                    clone.GetComponent<IPoolable>().OnGameObjectDisabled += OnObjectUseEnded;
                }
            }
            else
            {
                Debug.LogError("ERROR: Failed to create pool(Prefab is invalid)!!!");
            }
        }
    }

    public override void InitObjectOnReuse(GameObject obj, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        obj.SetActive(true);
        obj.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) obj.transform.parent = transform;
    }

    public GameObject ReuseObjectFast(int ID, Vector3 position)
    {
        emptyTransform.position = position;
        return ReuseObject(ID, emptyTransform, Quaternion.identity, false);
    }

    public override void ResetPool()
    {
        int[] keys = objects.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
            Destroy(objects[keys[i]]);
        base.ResetPool();
    }
}