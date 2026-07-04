using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class StartGraffitiAction : StateAction
    {
        public bool apply;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IGraffitiable graffitiable))
            {
                GameManager.instance.graffitiManager.StartGraffiti(graffitiable.GraffitiTinkerOffset + (Vector2)stateController.transform.position);
            }
        }
    }
}