using UnityEngine;

[CreateAssetMenu(fileName = "SaveCardsState", menuName = "Scriptable Objects/Save/States/SaveCardsState", order = 3)]
public class SaveCardsStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.SaveCards();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}