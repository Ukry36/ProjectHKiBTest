using UnityEngine;

[CreateAssetMenu(fileName = "SaveGearsState", menuName = "Scriptable Objects/Save/States/SaveGearsState", order = 2)]
public class SaveGearsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.SaveGears();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}