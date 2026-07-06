using System;
using UnityEngine;

[Serializable]
public class AnimationInitializeInfo
{
    public EventFlagSO eventFlag;
    public int eventFlagCondition;
    public Vector3 position;
    public EnumManager.AnimDir dir;
    public string animationName;
}

public class EventControllableAnimation : EventControllableBase<SimpleAnimationPlayer>
{

    public AnimationInitializeInfo[] InitInfos { get; set; }

    public override void Initialize()
    {
        EventManager eventManager = GameManager.instance.eventManager;
        for (int i = 0; i < InitInfos.Length; i++)
        {
            if (eventManager.eventFlags.ContainsKey(InitInfos[i].eventFlag)
             && eventManager.eventFlags[InitInfos[i].eventFlag] == InitInfos[i].eventFlagCondition)
            {
                Target.Initialize();
                Target.transform.position = InitInfos[i].position;
                Target.SetDirection(InitInfos[i].dir);
                Target.Play(InitInfos[i].animationName);
            }
        }
    }
}