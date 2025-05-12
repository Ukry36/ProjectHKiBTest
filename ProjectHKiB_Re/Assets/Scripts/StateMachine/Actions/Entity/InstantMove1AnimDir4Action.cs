using UnityEngine;
[CreateAssetMenu(fileName = "InstantMove1AnimDir4Action", menuName = "Scriptable Objects/State Machine/Action/InstantMove1AnimDir4", order = 3)]
public class InstantMove1AnimDir4Action : StateActionSO
{
    [SerializeField] private MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IEntityStateController controller))
        {
            movementManager.InstantMove
            (
                stateController.transform,
                movable,
                (Vector3)controller.AnimationController.LastSetAnimationDir4
            );
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
