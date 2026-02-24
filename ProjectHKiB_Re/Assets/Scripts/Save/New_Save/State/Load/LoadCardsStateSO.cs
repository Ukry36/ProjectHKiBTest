using UnityEngine;

[CreateAssetMenu(fileName = "LoadCardsState", menuName = "Scriptable Objects/Save/States/LoadCardsState", order = 4)]
public class LoadCardsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.LoadCards();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}