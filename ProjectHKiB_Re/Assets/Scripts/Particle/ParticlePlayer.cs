using System.Collections.Generic;
using UnityEngine;
public class ParticlePlayer : MonoBehaviour
{
    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;
    [HideInInspector] public ParticleSystem mainParticleSystem; //subSystems are also controlled by this
    private int ID;
    [field: SerializeField] public int PoolSize { get; set; }

    public void Awake()
    {
        if (TryGetComponent(out ParticleSystem component))
        {
            mainParticleSystem = component;
        }
        else Debug.LogError("ERROR: MainParticleSystem is missing!!!");
    }

    //Will probably be played only in pooling sequence
    public void InitializeFromPool(ParticlePlayer particlePlayer)
    {
        ID = particlePlayer.GetInstanceID();
        this.gameObject.SetActive(true);
    }

    public void OnParticleSystemStopped()
    {
        OnGameObjectDisabled?.Invoke(ID, this.GetInstanceID());
    }
}
