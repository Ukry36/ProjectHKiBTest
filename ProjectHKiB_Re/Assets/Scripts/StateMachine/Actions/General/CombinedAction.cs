using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CombinedAction : StateAction
    {
        [SerializeField] private StateAction[] actions;

        public override void Act(StateController stateController)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] != null) actions[i].Act(stateController);
            }
        }
    }
}