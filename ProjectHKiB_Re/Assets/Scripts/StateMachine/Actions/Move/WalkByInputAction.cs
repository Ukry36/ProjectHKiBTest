using UnityEngine;
[CreateAssetMenu(fileName = "WalkByInput", menuName = "State Machine/Action/Move/WalkByInputAction")]
public class WalkByInputAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            movable.IsWalking = true;
            movable.WalkingDir = GameManager.instance.inputManager.MoveInput;
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
