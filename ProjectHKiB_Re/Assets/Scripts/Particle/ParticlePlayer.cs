using System.Collections.Generic;
using UnityEngine;
public class ParticlePlayer : MonoBehaviour, IPoolable
{
    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;
    public ParticleSystem mainParticleSystem; //subSystems are also controlled by this
    private ParticleDataSO _particleData;

    public int PoolSize { get; set; }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_particleData)
            InitializeFromPool(_particleData);
    }

    //Will probably be played only in pooling sequence
    public void InitializeFromPool(ParticleDataSO particleData)
    {
        _particleData = particleData;
        var clone = Instantiate(_particleData.mainParticlePrefab, this.transform);
        for (int i = 0; i < _particleData.subParticlePrefabs.Count; i++)
            Instantiate(_particleData.subParticlePrefabs[i], clone.transform);
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(_particleData.ID, this.gameObject.GetHashCode());
    }
}
