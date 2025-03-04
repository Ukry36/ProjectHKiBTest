using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class ParticleManager : PoolManager<ParticlePlayer>
{
    [SerializeField] private ParticleDataSO[] allDatas;

    private Queue<Tuple<ParticleDataSO, int, Vector3>> _simpleParticleEmitterQueue;
    private bool _simpleParticleEmitterDequeueInProgress;
    private ParticlePlayer _simpleParticleEmitter;

    public override void Initialize()
    {
        _simpleParticleEmitterQueue = new();
        base.Initialize();
    }

    public override void CreatePool()
    {
        base.CreatePool();

        var clone = Instantiate(prefab, this.transform);
        if (clone.TryGetComponent(out ParticlePlayer simpleParticleEmitter))
            _simpleParticleEmitter = simpleParticleEmitter;
        else
            Debug.LogError("ERROR: Failed to create simpleParticleEmitter(ParticlePlayer prefab is invalid)!!!");

        for (int i = 0; i < allDatas.Length; i++)
        {
            for (int j = 0; j < allDatas[i].PoolSize; j++)
            {
                clone = Instantiate(prefab, this.transform);
                if (clone.TryGetComponent(out ParticlePlayer particlePlayer))
                {
                    AddPool(allDatas[i].ID, particlePlayer);
                    particlePlayer.InitializeFromPool(allDatas[i]);
                    particlePlayer.OnGameObjectDisabled += OnGameObjectDisabled;
                }
                else
                {
                    Debug.LogError("ERROR: Failed to create pool(ParticlePlayer prefab is invalid)!!!");
                }
            }
        }
    }

    public override void SetObjectOnReuse(ParticlePlayer particlePlayer, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        particlePlayer.gameObject.SetActive(true);
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

    public void EmitParticle(ParticleDataSO particleData, int emitCount, Vector3 pos)
    {
        _simpleParticleEmitterQueue.Enqueue(Tuple.Create(particleData, emitCount, pos));
        if (!_simpleParticleEmitterDequeueInProgress)
        {
            _simpleParticleEmitterDequeueInProgress = true;
            StartCoroutine(EmitParticleDequeueCoroutine());
        }
    }

    private void EmitParticleDequeue(ParticleDataSO particleData, int emitCount, Vector3 pos)
    {
        _simpleParticleEmitter.InitializeFromPool(particleData);
        _simpleParticleEmitter.transform.position = pos;
        _simpleParticleEmitter.mainParticleSystem.Emit(emitCount);
    }

    private IEnumerator EmitParticleDequeueCoroutine()
    {
        while (_simpleParticleEmitterQueue.Count > 0)
        {
            Tuple<ParticleDataSO, int, Vector3> tuple = _simpleParticleEmitterQueue.Dequeue();
            EmitParticleDequeue(tuple.Item1, tuple.Item2, tuple.Item3);
            yield return null;
        }
        _simpleParticleEmitterDequeueInProgress = false;
    }

    public ParticlePlayer PlayParticle(int ID, Transform transform, bool attatchToTransform)
    {
        ParticlePlayer clone = ReuseObject(ID, transform, quaternion.identity, attatchToTransform);
        if (!clone)
        {
            Debug.LogError("ERROR: Failed to play Particle(failed to reuse object)!!! ID: " + ID);
            return null;
        }
        clone.mainParticleSystem.Play();
        return clone;
    }

    public void StopPlaying(ParticlePlayer particlePlayer) => particlePlayer.gameObject.SetActive(false);

    public override void ResetPool()
    {
        int[] keys = objectPool.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
            Destroy(objectPool[keys[i]].gameObject);
        base.ResetPool();
        Destroy(_simpleParticleEmitter.gameObject);
        _simpleParticleEmitter = null;
    }
}