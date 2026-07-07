using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "Event/MapData")]
public class MapDataSO : ScriptableObject
{
    [NaughtyAttributes.Scene]
    public string mapAddressableID;
    public string bgmID;
}