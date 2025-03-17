using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "Player Stop Walk Decision", menuName = "Scriptable Objects/State Machine/Decision/Player Stop Walk Decision", order = 4)]
public class PlayerStopWalkDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        var movable = stateController.GetInterface<IGridMovable>();
        if (movable != null)
        {
            return movable.MovePoint.transform.position.Equals(stateController.transform.position)
            && InputManager.instance.MoveInput.Equals(Vector2.zero);
        }
        else
        {
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}
