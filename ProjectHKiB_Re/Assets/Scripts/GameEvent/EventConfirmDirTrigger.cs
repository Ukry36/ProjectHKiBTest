using UnityEngine;

public class EventConfirmDirTrigger : GameEventTrigger
{
    [SerializeField] private MathManagerSO mathManager;
    [SerializeField] private Vector2 requiredDir;

    public override void UpdateTrigger()
    {
        int length = _collider2D.OverlapCollider(_contactFilter, colliders);
        if (length > 0)
        {
            if (_canTrigger && GameManager.instance.inputManager.ConfirmInput
                && mathManager.IsVector2HasComponent(GameManager.instance.inputManager.LastSetMoveInput, requiredDir))
            {
                GameManager.instance.inputManager.LastSetMoveInput = Vector2.zero;
                Event.RegisterTarget(colliders[0].transform);
                Event.TriggerEvent();
                CoolTime();
            }
        }

    }
}