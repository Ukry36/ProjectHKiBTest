using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "GraffitiEndedDecision", menuName = "State Machine/Decision/Graffiti/GraffitiEndedDecision")]
public class GraffitiEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        return GameManager.instance.graffitiManager.IsGraffitiEnded;
    }
}
