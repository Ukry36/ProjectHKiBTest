using UnityEngine;
[CreateAssetMenu(fileName = "WalkByLastSetAnimationDir", menuName = "Scriptable Objects/State Machine/Action/WalkByLastSetAnimationDirAction", order = 3)]
public class WalkByLastSetAnimationDirAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            movementManager.WalkMove(stateController.transform, movable, stateController.animationController.LastSetAnimationDir8);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}


