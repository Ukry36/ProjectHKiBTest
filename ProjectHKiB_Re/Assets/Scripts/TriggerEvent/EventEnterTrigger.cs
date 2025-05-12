using UnityEngine;

public class EventEnterTrigger : EventTrigger
{
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!EventController.CurrentTarget && ((1 << collision.gameObject.layer) & _interactLayer) != 0)
        {
            EventController.RegisterTarget(collision.transform);
            EventController.TriggerEvent();
        }
    }
}