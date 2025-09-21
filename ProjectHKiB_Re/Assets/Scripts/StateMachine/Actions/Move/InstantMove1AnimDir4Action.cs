using UnityEngine;
[CreateAssetMenu(fileName = "InstantMove1AnimDir4Action", menuName = "State Machine/Action/Move/InstantMove1AnimDir4")]
public class InstantMove1AnimDir4Action : StateActionSO
{
    [SerializeField] private MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            movementManager.InstantMove
            (
                stateController.transform,
                movable,
                (Vector3)animatable.LastSetAnimationDir4
            );
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
