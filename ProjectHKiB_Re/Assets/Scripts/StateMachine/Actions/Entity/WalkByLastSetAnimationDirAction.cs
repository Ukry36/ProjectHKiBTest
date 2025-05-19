using UnityEngine;
[CreateAssetMenu(fileName = "WalkByLastSetAnimationDir", menuName = "Scriptable Objects/State Machine/Action/WalkByLastSetAnimationDirAction", order = 3)]
public class WalkByLastSetAnimationDirAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IEntityStateController controller))
        {
            movementManager.WalkMove(stateController.transform, movable, controller.AnimationController.LastSetAnimationDir8);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}


