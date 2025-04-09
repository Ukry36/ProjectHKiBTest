using UnityEngine;
[CreateAssetMenu(fileName = "Audio Data", menuName = "Scriptable Objects/Data/Audio Data", order = 4)]
public class AudioDataSO : ScriptableObject
{
    public AudioTypeSO type;
    public AudioClip[] audioClips;
}
