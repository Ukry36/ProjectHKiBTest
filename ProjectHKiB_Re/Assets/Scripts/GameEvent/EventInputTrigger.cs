using UnityEngine;

public class EventInputTrigger : EventTrigger
{
    [SerializeField] private EnumManager.InputType _inputType;
    public override void UpdateTrigger()
    {
        int length = _collider2D.OverlapCollider(_contactFilter, colliders);
        if (length > 0)
        {
            if (_canTrigger && GameManager.instance.inputManager.GetInputByEnum(_inputType))
            {
                Event.RegisterTarget(colliders[0].transform);
                Event.TriggerEvent();
                CoolTime();
            }
        }
    }
}