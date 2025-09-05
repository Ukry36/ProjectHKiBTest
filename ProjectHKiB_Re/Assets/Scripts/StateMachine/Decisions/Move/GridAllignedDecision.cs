using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "GridAllignedDecision", menuName = "State Machine/Decision/Move/GridAllignedDecision")]
public class GridAllignedDecision : StateDecisionSO
{
    [SerializeField] private MathManagerSO mathManager;
    public override bool Decide(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            return mathManager.Absolute
            (
                Vector3.Distance(movable.MovePoint.transform.position, stateController.transform.position)
            ).Equals(0);
        }
        else
        {
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}
