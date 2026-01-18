using Assets.Scripts.Interfaces.Modules;
using UnityEngine;
[CreateAssetMenu(fileName = "GraffitiMoveByInput", menuName = "State Machine/Action/Move/GraffitiMoveByInput")]
public class GraffitiMoveByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            if (GameManager.instance.inputManager.DInput) 
                MoveAndProcessGraffiti(stateController.transform, movable, Vector2.down);
            else if (GameManager.instance.inputManager.LInput) 
                MoveAndProcessGraffiti(stateController.transform, movable, Vector2.left);
            else if (GameManager.instance.inputManager.RInput) 
                MoveAndProcessGraffiti(stateController.transform, movable, Vector2.right);
            else if (GameManager.instance.inputManager.UInput) 
                MoveAndProcessGraffiti(stateController.transform, movable, Vector2.up);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
        
    }

    private void MoveAndProcessGraffiti(Transform transform, IMovable movable, Vector2 dir)
    {
        movementManager.InstantMove(transform, movable, dir);
        GameManager.instance.graffitiManager.ProcessGraffiti(transform.position);
    }
}
