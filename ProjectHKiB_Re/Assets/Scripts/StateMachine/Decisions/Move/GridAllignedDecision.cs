using UnityEngine;
[CreateAssetMenu(fileName = "GridAllignedDecision", menuName = "State Machine/Decision/Move/GridAllignedDecision")]
public class GridAllignedDecision : StateDecisionSO
{
    [SerializeField] private MathManagerSO mathManager;
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IPhysics phys)) return phys.Mode == MovementMode.Grid;
        else
        {
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}
