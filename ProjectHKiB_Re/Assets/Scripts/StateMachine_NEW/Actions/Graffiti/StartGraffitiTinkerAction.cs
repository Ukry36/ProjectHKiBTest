using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class StartGraffitiTinkerAction : StateAction
    {
        public bool apply;
        public override void Act(StateController stateController)
        {
            GameManager.instance.graffitiManager.StartTinker();
        }
    }
}