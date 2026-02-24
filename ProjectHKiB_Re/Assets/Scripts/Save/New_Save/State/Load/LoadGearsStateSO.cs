using UnityEngine;

[CreateAssetMenu(fileName = "LoadGearsState", menuName = "Scriptable Objects/Save/States/LoadGearsState", order = 2)]
public class LoadGearsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.LoadGears();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}