using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "Event/MapData")]
public class MapDataSO : ScriptableObject
{
    [NaughtyAttributes.Scene]
    public string mapAddressableID;
    public string bgmID;
    public SerializedDictionary<string, List<EntityInitializeInfo>> allEntityInitInfos;
    public SerializedDictionary<string, List<AnimationInitializeInfo>> allAnimInitInfos;
}