using UnityEngine;
[CreateAssetMenu(fileName = "Apply Sprint Action", menuName = "Scriptable Objects/State Machine/Action/Apply Sprint", order = 3)]
public class ApplySprintAction : StateActionSO
{
    public bool apply;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IGridMovable>();
        var player = stateController.GetInterface<Player>();
        if (movable != null && player != null)
        {
            movable.Speed.Value = apply ? movable.Speed.Value * movable.SprintCoeff.Value : player.playerData.Speed.Value;
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}