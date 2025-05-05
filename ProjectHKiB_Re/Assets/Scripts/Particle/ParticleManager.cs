using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class ParticleManager : PoolManager<ParticlePlayer>
{
    [SerializeField] private ParticlePlayer[] allDatas;

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
        GameObject clone;
        for (int i = 0; i < allDatas.Length; i++)
        {
            CreatePool(allDatas[i].GetInstanceID(), allDatas[i].PoolSize);
            for (int j = 0; j < allDatas[i].PoolSize; j++)
            {
                clone = Instantiate(allDatas[i].gameObject, this.transform);
                if (clone.TryGetComponent(out ParticlePlayer particlePlayer))
                {
                    AddObjectToPool(allDatas[i].GetInstanceID(), particlePlayer);
                    particlePlayer.InitializeFromPool(allDatas[i]);
                    particlePlayer.OnGameObjectDisabled += OnObjectUseEnded;
                }
                else
                {
                    Debug.LogError("ERROR: Failed to create pool(ParticlePlayer prefab is invalid)!!!");
                }
            }
        }
    }

    public override void InitObjectOnReuse(ParticlePlayer particlePlayer, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        particlePlayer.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) particlePlayer.transform.parent = transform;
    }

    public ParticlePlayer GetParticlePlayer(int ID, Transform transform, bool attatchToTransform)
    {
        var clone = ReuseObject(ID, transform, quaternion.identity, attatchToTransform);
        if (clone.TryGetComponent(out ParticlePlayer ParticlePlayer))
        {
            return ParticlePlayer;
        }
        else
        {
            Debug.LogError("ERROR: Failed to get ParticlePlayer(ParticlePlayer prefab is invalid)!!!");
            return null;
        }
    }

    public ParticlePlayer PlayParticle(int ID, Transform transform, bool attatchToTransform)
    {
        ParticlePlayer clone = ReuseObject(ID, transform, quaternion.identity, attatchToTransform);
        if (!clone)
        {
            Debug.LogError("ERROR: Failed to play Particle(failed to reuse object)!!! ID: " + ID);
            return null;
        }
        //Debug.Log("particle played: " + clone.name);
        clone.mainParticleSystem.Play();
        return clone;
    }

    public ParticlePlayer PlayParticleOneShot(int ID, Transform transform)
    {
        ParticlePlayer clone = ReuseObject(ID, transform, quaternion.identity, false);
        if (!clone)
        {
            Debug.LogError("ERROR: Failed to play Particle(failed to reuse object)!!! ID: " + ID);
            return null;
        }
        //Debug.Log("particle played: " + clone.name);
        clone.mainParticleSystem.Play();
        return clone;
    }

    public void StopPlaying(int ID) => GetObject(ID).mainParticleSystem.Stop();

    public override void ResetPool()
    {
        if (objects != null && objects.Count > 0)
        {
            int[] keys = objects.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                objects[keys[i]].OnGameObjectDisabled -= OnObjectUseEnded;
                Destroy(objects[keys[i]].gameObject);
            }

        }
        base.ResetPool();
    }
}