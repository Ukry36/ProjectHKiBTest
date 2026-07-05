using UnityEngine;

public class EventTarget
{
    public string ID;
    public EventManager.TargetSearchType targetSearchType;
}

[CreateAssetMenu(fileName = "Event", menuName = "Event/Event")]
public class EventSO : StateMachineSO
{
    public EventTarget[] targets;
    public MapDataSO mapPreset;
}