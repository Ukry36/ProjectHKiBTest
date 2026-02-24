using UnityEngine;

[CreateAssetMenu(fileName = "SaveEventsState", menuName = "Scriptable Objects/Save/States/SaveEventsState", order = 4)]
public class SaveEventsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.SaveEvents();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}