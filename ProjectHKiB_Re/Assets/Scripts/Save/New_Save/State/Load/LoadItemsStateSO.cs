using UnityEngine;

[CreateAssetMenu(fileName = "LoadItemsState", menuName = "Scriptable Objects/Save/States/LoadItemsState", order = 1)]
public class LoadItemsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.LoadItems();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}