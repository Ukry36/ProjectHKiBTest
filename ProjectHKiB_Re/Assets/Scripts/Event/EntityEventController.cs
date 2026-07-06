using System;
using UnityEngine;

[Serializable]
public class EntityInitializeInfo
{
    public EventFlagSO eventFlag;
    public int eventFlagCondition;
    public Vector3 position;
    public EnumManager.AnimDir dir;
    public StateMachineSO stateMachine;
    public StateSO state;
}

public class EventControllableEntity : EventControllableBase<StateController>
{
    public EntityInitializeInfo[] InitInfos { get; set; }

    public override void Initialize()
    {
        EventManager eventManager = GameManager.instance.eventManager;
        for (int i = 0; i < InitInfos.Length; i++)
        {
            if (eventManager.eventFlags.ContainsKey(InitInfos[i].eventFlag)
             && eventManager.eventFlags[InitInfos[i].eventFlag] == InitInfos[i].eventFlagCondition)
            {
                Target.Initialize();
                Target.Initialize(InitInfos[i].stateMachine);
                Target.ChangeState(InitInfos[i].state);
                if (Target.TryGetInterface(out IPhysics phys)) phys.RealTeleport(InitInfos[i].position);
                else Target.transform.position = InitInfos[i].position;
                if (Target.TryGetInterface(out IDirAnimatable dirAnimatable)) dirAnimatable.SetAnimationDirection(InitInfos[i].dir);
            }
        }
    }
}