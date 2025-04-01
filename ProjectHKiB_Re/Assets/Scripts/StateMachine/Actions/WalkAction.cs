using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Walk Action", menuName = "Scriptable Objects/State Machine/Action/Walk", order = 3)]
public class WalkAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        var movable = stateController.GetInterface<IMovable>();
        if (movable != null)
        {
            movementManager.WalkMove(stateController.transform, movable.MovePoint.transform, InputManager.instance.MoveInput, movable.WallLayer);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
