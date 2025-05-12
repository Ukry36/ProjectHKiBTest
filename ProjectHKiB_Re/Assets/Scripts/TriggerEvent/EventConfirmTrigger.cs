using UnityEngine;

public class EventConfirmTrigger : EventTrigger
{
    public void OnTriggerStay2D(Collider2D collision)
    {
        if (!EventController.CurrentTarget && ((1 << collision.gameObject.layer) & _interactLayer) != 0)
        {
            if (GameManager.instance.inputManager.ConfirmInput)
            {
                EventController.RegisterTarget(collision.transform);
                EventController.TriggerEvent();
            }
        }
    }
}