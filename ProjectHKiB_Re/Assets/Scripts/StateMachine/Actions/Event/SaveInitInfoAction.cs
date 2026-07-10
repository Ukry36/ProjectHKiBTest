using System.Linq;
using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SaveInitInfoAction : StateAction
    {
        public string[] targetIDs;
        public EventFlagSO flag;
        public int flagValue;
        public override void Act(StateController stateController)
        {
#if UNITY_EDITOR
            if (stateController.TryGetInterface(out IEvent @event))
            {
                for (int i = 0; i < targetIDs.Length; i++)
                {
                    if (!@event.CurrentTargets.targetEntities.ContainsKey(targetIDs[i])) continue;
                    EventControllableEntity target = @event.CurrentTargets.targetEntities[targetIDs[i]];
                    target.SaveCurrentStateToInitInfo(flag, flagValue);
                }

                for (int i = 0; i < targetIDs.Length; i++)
                {
                    if (!@event.CurrentTargets.targetAnimations.ContainsKey(targetIDs[i])) continue;
                    EventControllableAnimation target = @event.CurrentTargets.targetAnimations[targetIDs[i]];
                    target.SaveCurrentStateToInitInfo(flag, flagValue);
                }
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
#endif
        }
    }
}