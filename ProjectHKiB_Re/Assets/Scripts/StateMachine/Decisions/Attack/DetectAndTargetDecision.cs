using UnityEngine;
[CreateAssetMenu(fileName = "DetectAndTarget", menuName = "State Machine/Decision/Attack/DetectAndTargetDecision")]
public class DetectAndTargetDecision : StateDecisionSO
{
    [SerializeField] private float radius;
    [SerializeField] private TargetingManagerSO targetManager;
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out ITargetable targetable))
        {
            Transform t = targetManager.PositianalTarget(stateController.transform.position, radius, targetable.TargetLayers);
            targetable.CurrentTarget = t;
            return t;
        }
        return false;
    }
}
