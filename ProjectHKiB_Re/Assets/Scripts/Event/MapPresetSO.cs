using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "Event", menuName = "Event/MapData")]
public class MapDataSO : ScriptableObject
{
    public Scene Map;
    public string mapID;
    public string bgmID;

    public SerializedDictionary<string, IEventControllable> EventTargets;
    // stores cirtain map's entity position, direction and state/animation state. Also stores some of map animation state info.

    // need to think about how to implement this.. 
}