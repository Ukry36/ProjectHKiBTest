using UnityEngine;

[CreateAssetMenu(
    fileName = "LoadReadSucceededDecision",
    menuName = "Scriptable Objects/Save/Decisions/LoadReadSucceededDecision",
    order = 0)]
public class LoadReadSucceededDecisionSO : StateDecisionSO
{
    public override bool Decide(StateController controller)
    {
        var module = controller.GetInterface<SaveModule>();
        if (module == null) return false;

        // ReadSaveFile() 실패 시 LoadedData = null 로 세팅되어 있음
        return module.LoadedData != null;
    }
}