using UnityEngine;

[CreateAssetMenu(fileName = "SaveItemsState", menuName = "Scriptable Objects/Save/States/SaveItemsState", order = 1)]
public class SaveItemsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.SaveItems();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}