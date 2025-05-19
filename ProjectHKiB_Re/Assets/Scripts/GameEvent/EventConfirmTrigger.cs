public class EventConfirmTrigger : EventTrigger
{
    private void Update()
    {
        if (enabled)
        {
            int length = _collider2D.OverlapCollider(_contactFilter, colliders);
            if (length > 0)
            {
                if (_canTrigger && GameManager.instance.inputManager.ConfirmInput)
                {
                    Event.RegisterTarget(colliders[0].transform);
                    Event.TriggerEvent();
                    CoolTime();
                }
            }
        }

    }
}