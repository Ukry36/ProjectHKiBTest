using System;
using UnityEngine;

[Serializable]
public class EventTargetSearchInfo
{
    public string ID;
    public EventManager.TargetSearchType targetSearchType;
}

[CreateAssetMenu(fileName = "Event", menuName = "Event/Event")]
public class EventSO : StateMachineSO
{
    public EventTargetSearchInfo[] involvedEventTargets;
    //public MapDataSO mapData;
}
