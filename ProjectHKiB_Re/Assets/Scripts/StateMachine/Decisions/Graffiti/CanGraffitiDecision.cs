using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "CanGraffitiDecision", menuName = "State Machine/Decision/Graffiti/CanGraffitiDecision")]
public class CanGraffitiDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        return GameManager.instance.graffitiManager.CanGraffiti;
    }
}
