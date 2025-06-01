using UnityEngine;
[CreateAssetMenu(fileName = "WalkByInput", menuName = "Scriptable Objects/State Machine/Action/Move/WalkByInputAction")]
public class WalkByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            movementManager.WalkMove(stateController.transform, movable, movable.Speed, GameManager.instance.inputManager.MoveInput, movable.WallLayer);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
