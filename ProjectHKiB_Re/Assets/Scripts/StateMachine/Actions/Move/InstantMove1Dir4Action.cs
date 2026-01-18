using UnityEngine;
[CreateAssetMenu(fileName = "InstantMove1AnimDir4Action", menuName = "State Machine/Action/Move/InstantMove1AnimDir4")]
public class InstantMove1Dir4Action : StateActionSO
{
    [SerializeField] private MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            movementManager.InstantMove
            (
                stateController.transform,
                movable,
                movable.LastSetDir
            );
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
