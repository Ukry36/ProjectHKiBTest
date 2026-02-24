using UnityEngine;

[CreateAssetMenu(fileName = "SaveWriteFileState", menuName = "Scriptable Objects/Save/States/SaveWriteFileState", order = 5)]
public class SaveWriteFileStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module) => module.WriteSaveFile();
    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}