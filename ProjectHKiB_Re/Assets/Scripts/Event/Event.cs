using System.Collections.Generic;
using UnityEngine;

public class EventBlock
{
    public StateSO state;
    public EventTransition[] nextEvents;
}

public class EventTransition
{
    public EventFlagSO[] flags;
    public EventBlock nextEvent;
}

[CreateAssetMenu(fileName = "Event", menuName = "Event/Event")]
public class EventSO : ScriptableObject
{
    public EventBlock rootEventBlock;
}