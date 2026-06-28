using AYellowpaper.SerializedCollections;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public SerializedDictionary<EventVariableSO, bool> boolEventVariables;
    public SerializedDictionary<EventVariableSO, int> intEventVariables;
    public SerializedDictionary<EventVariableSO, float> floatEventVariables;

    public void StartEvent()
    {
        
    }
}