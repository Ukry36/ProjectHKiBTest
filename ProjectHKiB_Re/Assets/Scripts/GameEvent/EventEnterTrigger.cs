using UnityEngine;

public class EventEnterTrigger : EventTrigger
{
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (_canTrigger && !Event.CurrentTarget && ((1 << collision.gameObject.layer) & _contactFilter.layerMask) != 0)
        {
            Event.RegisterTarget(collision.transform);
            Event.TriggerEvent();
            CoolTime();
        }
    }
}