using UnityEngine;
[CreateAssetMenu(fileName = "Audio Data", menuName = "Scriptable Objects/Data/Audio Data", order = 4)]
public class AudioDataSO : ScriptableObject, IID
{
    [field: SerializeField] public int ID { get; set; }
    [field: SerializeField] public string Name { get; set; }

    public AudioTypeSO type;
    public AudioClip[] audioClips;
}
