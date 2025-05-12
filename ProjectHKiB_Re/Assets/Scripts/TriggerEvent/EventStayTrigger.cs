using UnityEngine;

public class EventStayTrigger : EventTrigger
{
    public void OnTriggerStay2D(Collider2D collision)
    {
        if (!EventController.CurrentTarget && ((1 << collision.gameObject.layer) & _interactLayer) != 0)
        {
            EventController.RegisterTarget(collision.transform);
            EventController.TriggerEvent();
        }
    }
}