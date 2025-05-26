public class EventStayTrigger : EventTrigger
{
    public override void UpdateTrigger()
    {
        int length = _collider2D.OverlapCollider(_contactFilter, colliders);
        if (length > 0)
        {
            if (_canTrigger)
            {
                Event.RegisterTarget(colliders[0].transform);
                Event.TriggerEvent();
                CoolTime();
            }
        }
    }
}