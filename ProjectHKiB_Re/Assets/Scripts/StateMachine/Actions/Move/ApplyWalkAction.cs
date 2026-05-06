using UnityEngine;
[CreateAssetMenu(fileName = "Apply Walk Action", menuName = "State Machine/Action/Move/Apply Walk")]
public class ApplyWalkAction : StateActionSO
{
    public bool apply;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            movable.IsWalking = apply;
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}