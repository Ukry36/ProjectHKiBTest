using UnityEngine;
[CreateAssetMenu(fileName = "Apply Sprint Action", menuName = "Scriptable Objects/State Machine/Action/Apply Sprint", order = 3)]
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