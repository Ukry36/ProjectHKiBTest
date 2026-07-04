using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class TeleportEventAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out ITeleportEventable teleport) && stateController.TryGetInterface(out IEvent @event))
            {
                foreach (Collider2D col in @event.CurrentTargets)
                {
                    Transform transform = col.transform;
                    if (transform.TryGetComponent(out IPhysics phys)) phys.RealTeleport(teleport.Destination.position);

                    if (transform.TryGetComponent(out IDirAnimatable dirAnimatable)) dirAnimatable.SetAnimationDirection(teleport.EndDir);

                    if (transform.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        GameManager.instance.cameraManager.StrictMovement
                        (
                            teleport.Destination.position,
                            GameManager.instance.cameraManager.GetCurrentCameraPos()
                        );
                    }
                }
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}