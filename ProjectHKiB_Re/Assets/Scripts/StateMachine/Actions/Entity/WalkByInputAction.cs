using UnityEngine;
[CreateAssetMenu(fileName = "WalkByInput", menuName = "Scriptable Objects/State Machine/Action/WalkByInputAction", order = 3)]
public class WalkByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            movementManager.WalkMove(stateController.transform, movable, GameManager.instance.inputManager.MoveInput);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
