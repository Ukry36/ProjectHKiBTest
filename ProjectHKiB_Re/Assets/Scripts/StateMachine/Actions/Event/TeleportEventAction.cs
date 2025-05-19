using UnityEngine;
[CreateAssetMenu(fileName = "TeleportEvent", menuName = "Scriptable Objects/State Machine/Action/TeleportEvent")]
public class TeleportEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out ITeleportEventable teleport) && stateController.TryGetInterface(out IEvent @event))
        {
            if (@event.CurrentTarget.TryGetComponent(out IMovable movable))
            {
                movable.MovePoint.transform.position = teleport.Destination.position;
                @event.CurrentTarget.transform.position = teleport.Destination.position;
                // move camera also!!
            }
            else Debug.Log("Entity " + @event.CurrentTarget.name + " has no such interface: movable");
            if (@event.CurrentTarget.TryGetComponent(out IDirAnimatable dirAnimatable))
            {
                dirAnimatable.AnimationController.SetAnimationDirection(teleport.EndDir);
                // move camera also!!
            }
            else Debug.Log("Entity " + @event.CurrentTarget.name + " has no such interface: movable");
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
