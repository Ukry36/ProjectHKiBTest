using UnityEngine;

[CreateAssetMenu(fileName = "LoadReadState", menuName = "Scriptable Objects/Save/States/LoadReadState", order = 0)]
public class LoadReadStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module)
    {
        bool ok = module.ReadSaveFile();
        // ok가 false면 Transition Decision에서 FailState로 보내는 구조 추천
    }

    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}