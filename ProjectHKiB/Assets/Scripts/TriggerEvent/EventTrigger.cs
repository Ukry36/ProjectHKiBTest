using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventTrigger : MonoBehaviour, IInteractable
{
    public Collider2D Trigger { get; set; }
    public UnityEvent Event { get; set; }
    public float TriggerCoolTime { get; set; }
}
