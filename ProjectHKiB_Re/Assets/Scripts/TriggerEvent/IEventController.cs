using UnityEngine;

public interface IEventController
{
    public Transform CurrentTarget { get; set; }
    public void RegisterTarget(Transform transform);
    public void TriggerEvent();
    public void EndEvent();
    public EventTrigger Trigger { get; set; }
    public void InitializeTrigger();
}