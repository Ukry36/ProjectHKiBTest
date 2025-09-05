using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "Audio Type", menuName = "Type/Audio Type")]
public class AudioTypeSO : ScriptableObject, IPoolable
{
    [field: SerializeField] public int PoolSize { get; set; }
    public UnityEvent<int, int> OnGameObjectDisabled { get; set; }
    public int ID { get; set; }

    public int initOutTime;
    public bool playOneShot;
    public bool loop;

    public void OnDisable() { }
}
