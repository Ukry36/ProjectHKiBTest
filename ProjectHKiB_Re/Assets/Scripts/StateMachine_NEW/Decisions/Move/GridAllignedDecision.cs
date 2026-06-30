using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class GridAllignedDecision : StateDecision
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
}