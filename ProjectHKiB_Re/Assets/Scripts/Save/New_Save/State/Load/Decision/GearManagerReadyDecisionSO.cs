using UnityEngine;

[CreateAssetMenu(
    fileName = "GearManagerReadyDecision",
    menuName = "Scriptable Objects/Save/Decisions/GearManagerReadyDecision",
    order = 1)]
public class GearManagerReadyDecisionSO : StateDecisionSO
{
    public override bool Decide(StateController controller)
    {
        var module = controller.GetInterface<SaveModule>();
        if (module == null) return false;

        return module.IsGearManagerReady;
    }
}