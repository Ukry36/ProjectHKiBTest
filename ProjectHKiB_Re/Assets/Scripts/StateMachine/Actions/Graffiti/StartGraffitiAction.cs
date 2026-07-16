using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class StartGraffitiAction : StateAction
    {
        public int targetSlot;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IGraffitiable graffitiable))
            {
                GameManager.instance.graffitiManager.StartGraffiti(targetSlot, graffitiable.GraffitiTinkerOffset + (Vector2)stateController.transform.position);
            }
        }
    }
}