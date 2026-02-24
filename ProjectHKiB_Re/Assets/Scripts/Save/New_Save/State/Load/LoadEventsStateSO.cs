using UnityEngine;

[CreateAssetMenu(fileName = "LoadEventsState", menuName = "Scriptable Objects/Save/States/LoadEventsState", order = 6)]
public class LoadEventsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.LoadEvents();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}