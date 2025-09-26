using UnityEngine;

public interface IEvent : IInitializable
{
    public Transform CurrentTarget { get; set; }
    public void RegisterTarget(Transform transform);
    public void TriggerEvent();
    public void EndEvent();
}