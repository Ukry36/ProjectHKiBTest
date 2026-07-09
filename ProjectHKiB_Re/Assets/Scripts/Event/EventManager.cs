using System;
using AYellowpaper.SerializedCollections;

[Serializable]
public class EventTargets
{
    public EventTargets()
    {
        targetEntities = new();
        targetAnimations = new();
    }
    public SerializedDictionary<string, EventControllableEntity> targetEntities;
    public SerializedDictionary<string, EventControllableAnimation> targetAnimations;
}

public class EventManager : StateController
{
    public enum TargetSearchType { Player, FromMap, Manual }
    public SerializedDictionary<EventFlagSO, int> eventFlags;

    public EventTargets currentTargets;

    public void SetEventFlag(EventFlagSO flag, int value)
    {
        eventFlags ??= new();
        eventFlags[flag] = value;
    }

    public void StartEvent(EventSO eventSO, EventTargets manualTargets = null)
    {
        InitFindTargets(eventSO, manualTargets);
        Initialize(eventSO);
        if (TryGetInterface(out IEvent @event)) @event.CurrentTargets = currentTargets;
    }

    public void InitFindTargets(EventSO eventSO, EventTargets manualTargets)
    {
        currentTargets = new();
        for (int i = 0; i < eventSO.involvedEventTargets.Length; i++)
        {
            EventTargetSearchInfo target = eventSO.involvedEventTargets[i];
            if (target.targetSearchType == TargetSearchType.Player)
            {
                currentTargets.targetEntities[target.ID] = GameManager.instance.player.GetComponent<EventControllableEntity>();
            }
            else if (target.targetSearchType == TargetSearchType.FromMap)
            {
                MapLocalManager localManager = GameManager.instance.mapManager.localManager;
                if (localManager && localManager.allEventTargets.targetEntities.ContainsKey(target.ID))
                {
                    currentTargets.targetEntities[target.ID] = localManager.allEventTargets.targetEntities[target.ID];
                }
                else if (localManager && localManager.allEventTargets.targetAnimations.ContainsKey(target.ID))
                {
                    currentTargets.targetAnimations[target.ID] = localManager.allEventTargets.targetAnimations[target.ID];
                }
            }
            else if (target.targetSearchType == TargetSearchType.Manual && manualTargets != null)
            {
                currentTargets.targetEntities[target.ID] = manualTargets.targetEntities[target.ID];
                currentTargets.targetAnimations[target.ID] = manualTargets.targetAnimations[target.ID];
            }
        }
    }

}