using System.Linq;
using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SaveInitInfoAction : StateAction
    {
        public EventFlagSO flag;
        public int flagValue;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IEvent @event))
            {
                EventControllableEntity[] targets = @event.CurrentTargets.targetEntities.Values.ToArray();
                for (int i = 0; i < targets.Length; i++)
                {
                    EnumManager.AnimDir dir = EnumManager.AnimDir.D;
                    if (targets[i].Target.TryGetInterface(out IDirAnimatable animatable)) dir = animatable.AnimationDirection;

                    EntityInitializeInfo info = new()
                    {
                        eventFlag = flag,
                        eventFlagCondition = flagValue,
                        position = targets[i].Target.transform.position,
                        dir = dir,
                        stateMachine = targets[i].Target.StateMachine,
                        state = targets[i].Target.CurrentState
                    };
                    targets[i].InitInfos.Add(info);
                }
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}