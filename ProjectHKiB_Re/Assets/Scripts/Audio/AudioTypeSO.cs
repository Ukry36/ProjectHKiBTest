using UnityEngine;
[CreateAssetMenu(fileName = "Audio Type", menuName = "Scriptable Objects/Enum/Audio Type", order = 3)]
public class AudioTypeSO : ScriptableObject, IPoolable
{
    [field: SerializeField] public int PoolSize { get; set; }
    public int initOutTime;
    public bool playOneShot;
    public bool loop;

    public void OnDisable() { }
}
