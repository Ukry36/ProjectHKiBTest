using UnityEngine;

[CreateAssetMenu(fileName = "SaveInitState", menuName = "Scriptable Objects/Save/States/SaveInitState", order = 0)]
public class SaveInitStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.BeginSaveSession();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}