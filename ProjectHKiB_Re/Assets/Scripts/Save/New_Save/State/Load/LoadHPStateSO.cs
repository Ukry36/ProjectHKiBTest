using UnityEngine;

[CreateAssetMenu(fileName = "LoadHPState", menuName = "Scriptable Objects/Save/States/LoadHPState", order = 5)]
public class LoadHPStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.ApplyHP();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}