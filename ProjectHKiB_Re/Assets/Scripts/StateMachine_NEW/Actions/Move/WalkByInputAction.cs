using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WalkByInputAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IPhysics phys))
            {
                phys.IsWalking = true;
                phys.WalkingDir = GameManager.instance.inputManager.MoveInput;
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");

        }
    }
}