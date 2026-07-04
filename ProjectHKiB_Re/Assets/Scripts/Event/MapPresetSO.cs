using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "Event/MapPreset")]
public class MapPresetSO : ScriptableObject
{
    public SerializedDictionary<string, IEventControllable> EventTargets;
    // stores cirtain map's entity position, direction and state/animation state. Also stores some of map animation state info.

    // need to think about how to implement this.. 
}