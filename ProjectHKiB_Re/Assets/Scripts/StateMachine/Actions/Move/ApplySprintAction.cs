using UnityEngine;
[CreateAssetMenu(fileName = "Apply Sprint Action", menuName = "State Machine/Action/Move/Apply Sprint")]
public class ApplySprintAction : StateActionSO
{
    public bool apply;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            movable.IsSprinting = apply;
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}