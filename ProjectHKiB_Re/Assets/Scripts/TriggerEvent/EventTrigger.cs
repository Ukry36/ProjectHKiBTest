using UnityEngine;

public abstract class EventTrigger : MonoBehaviour
{
    public IEventController EventController { get; set; }
    [SerializeField] protected LayerMask _interactLayer;

    public void Initialize(IEventController eventController)
        => this.EventController = eventController;
}