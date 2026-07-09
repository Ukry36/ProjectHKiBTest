public class EventStayTrigger : GameEventTrigger
{
    public override void UpdateTrigger()
    {
        int length = _collider2D.OverlapCollider(_contactFilter, colliders);
        if (length > 0)
        {
            if (_canTrigger)
            {
                Event.TriggerEvent();
                CoolTime();
            }
        }
    }
}