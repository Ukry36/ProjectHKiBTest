using UnityEngine;

public interface IEvent
{
    public Transform CurrentTarget { get; set; }
    public void RegisterTarget(Transform transform);
    public void TriggerEvent();
    public void EndEvent();
}