using System.Linq;
using UnityEngine;
public class ObjectSpawnManager : PoolManager<GameObject>
{
    [SerializeField] private GameObject[] allDatas;

    public void Start()
    {
        Initialize();
    }
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
                CreatePool(allDatas[i].GetInstanceID(), poolable.PoolSize);
                for (int j = 0; j < poolable.PoolSize; j++)
                {
                    var clone = Instantiate(allDatas[i], this.transform);
                    AddObjectToPool(allDatas[i].GetInstanceID(), clone);
                    IPoolable p = clone.GetComponent<IPoolable>();
                    p.OnGameObjectDisabled.AddListener(OnObjectUseEnded);
                    p.ID = allDatas[i].GetInstanceID();
                    clone.SetActive(false);
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

    public GameObject SpawnObject(int ID, Transform transform, Quaternion rotation, bool attatchToTransform, bool record)
    {
        return ReuseObject(ID, transform, rotation, attatchToTransform, record);
    }

    public GameObject SpawnObjectSimple(int ID, Vector3 position, bool record)
    {
        _oneshotTransform.position = position;
        return ReuseObject(ID, _oneshotTransform, Quaternion.identity, false, record);
    }

    public override void ResetPool()
    {
        if (objects != null && objects.Count > 0)
        {
            int[] keys = objects.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
                Destroy(objects[keys[i]]);
        }
        base.ResetPool();
    }
}