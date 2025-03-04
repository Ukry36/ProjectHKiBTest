using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Particle Data", menuName = "Scriptable Objects/Data/Particle Data", order = 3)]
public class ParticleDataSO : ScriptableObject, IID, IPoolable
{
    [field: SerializeField] public int ID { get; set; }
    [field: SerializeField] public string Name { get; set; }
    [field: SerializeField] public int PoolSize { get; set; }
    public GameObject mainParticlePrefab;
    public List<GameObject> subParticlePrefabs = new();

    public void OnDisable() { Debug.Log("ScriptableObject OnDisable Called"); }
}
