using AYellowpaper.SerializedCollections;

public class EventManager : StateController
{
    public enum TargetSearchType { Player, PublicID, MapPreset, Manual }
    public SerializedDictionary<EventVariableSO, bool> boolEventVariables;
    public SerializedDictionary<EventVariableSO, int> intEventVariables;
    public SerializedDictionary<EventVariableSO, float> floatEventVariables;

    public SerializedDictionary<string, IEventControllable> currentTargets;

    public void StartEvent(EventSO eventSO, IEventControllable[] manualTargets)
    {
        Initialize(eventSO);
        InitFindTargets(eventSO, manualTargets);
    }

    public void InitFindTargets(EventSO eventSO, IEventControllable[] manualTargets)
    {
        currentTargets = new();
        int manualCount = 0;
        for (int i = 0; i < eventSO.targets.Length; i++)
        {
            EventTarget target = eventSO.targets[i];
            if (target.targetSearchType == TargetSearchType.Player)
            {
                currentTargets[target.ID] = GameManager.instance.player;
            }
            else if (target.targetSearchType == TargetSearchType.PublicID)
            {
                // search from public entity pool
            }
            else if (target.targetSearchType == TargetSearchType.MapPreset)
            {
                if (eventSO.mapPreset && eventSO.mapPreset.EventTargets.ContainsKey(target.ID))
                    currentTargets[target.ID] = eventSO.mapPreset.EventTargets[target.ID];
            }
            else if (target.targetSearchType == TargetSearchType.Manual)
            {
                currentTargets[target.ID] = manualTargets[manualCount++];
            }
        }
    }

}