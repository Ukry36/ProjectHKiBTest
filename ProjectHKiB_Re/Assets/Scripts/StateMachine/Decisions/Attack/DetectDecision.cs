using UnityEngine;
[CreateAssetMenu(fileName = "Detect", menuName = "State Machine/Decision/Attack/DetectDecision")]
public class DetectDecision : StateDecisionSO
{
    [SerializeField] private float radius;
    [SerializeField] private TargetingManagerSO targetManager;
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out ITargetable targetable))
        {
            return targetManager.PositianalTarget(stateController.transform.position, radius, targetable.TargetLayers);
        }
        return false;
    }
}
