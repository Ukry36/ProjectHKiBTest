using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class KeepDodgeMoveByInputAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IPhysics movable) && stateController.TryGetInterface(out IDodgeable dodgeable))
            {
                Debug.LogError("ERROR: Not Implemented!!!");

                //movementManager.WalkMove(stateController.transform, movable, dodgeable.BaseDodgeSpeed, GameManager.instance.inputManager.MoveInput, dodgeable.KeepDodgeWallLayer);
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}