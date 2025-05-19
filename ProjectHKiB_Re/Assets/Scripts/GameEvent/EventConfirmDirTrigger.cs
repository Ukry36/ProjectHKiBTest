using UnityEngine;

public class EventConfirmDirTrigger : EventTrigger
{
    [SerializeField] private MathManagerSO mathManager;
    [SerializeField] private Vector2 requiredDir;
    [SerializeField][NaughtyAttributes.ReadOnly] private Transform _transform;

    private void Update()
    {
        if (enabled)
        {
            int length = _collider2D.OverlapCollider(_contactFilter, colliders);
            if (length > 0)
            {
                if (_canTrigger && GameManager.instance.inputManager.ConfirmInput
                    && mathManager.IsVector2HasComponent(GameManager.instance.inputManager.LastSetMoveInput, requiredDir))
                {
                    Event.RegisterTarget(colliders[0].transform);
                    Event.TriggerEvent();
                    CoolTime();
                }
            }
        }
    }
}