using UnityEngine;
using UnityEngine.Events;

public interface IInteractable
{
    public Collider2D Trigger { get; set; }
    public UnityEvent Event { get; set; }
    public float TriggerCoolTime { get; set; }

    public void OnInteracted()
    {
        Event.Invoke();
        //TriggerCooltime~~
    }
}