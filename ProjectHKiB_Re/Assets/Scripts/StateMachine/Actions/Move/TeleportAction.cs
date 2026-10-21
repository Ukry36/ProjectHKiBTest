using UnityEngine;
namespace StateMachine
{
    // 대상을 지정 좌표로 옮긴다 — 컷신 중 배치, 기상 지점 배치 등.
    //
    // RealTeleport는 카메라까지 같이 따라붙는 "진짜" 순간이동이고, LogicalTeleport는 위치만
    // 옮긴다(암전 뒤에서 몰래 옮길 때). 암전 중에 옮기면서 카메라가 튀는 게 싫으면 logical을 켤 것.
    //
    // destinationID를 채우면 position 대신 그 ID의 이벤트 대상 위치로 옮긴다 — 공유 EventSO 하나를
    // 여러 텔레포터가 쓰면서 목적지는 각자 GameStateEvent의 Manual 대상으로 넘기려고(TeleportEventPair).
    //
    // nearest를 켜면 이벤트 대상 대신 씬에 켜져 있는 EventControllableEntity 중 ID가 destinationID인
    // 것들에서 가장 가까운 곳으로 옮긴다 — 낙하 복귀(FallRespawn)처럼 같은 ID의 지점이 여러 개라
    // ID 하나에 하나만 담기는 대상 사전으로는 못 고를 때. 떨어지던 속도는 도착 후에도 남으므로 끈다.
    [System.Serializable]
    public class TeleportAction : StateAction
    {
        public Vector3 position;
        public bool logical;
        public string destinationID;
        public bool nearest;

        public override void Act(StateController stateController)
        {
            if (!stateController.TryGetInterface(out IPhysics physics))
            {
                Debug.LogError("ERROR: TeleportAction - IPhysics를 찾을 수 없습니다.");
                return;
            }

            Vector3 target = position;
            if (nearest)
            {
                EventControllableEntity closest = null;
                float closestSqr = float.MaxValue;
                foreach (EventControllableEntity point in Object.FindObjectsOfType<EventControllableEntity>())
                {
                    if (point.ID != destinationID) continue;
                    float sqr = ((Vector2)(point.transform.position - stateController.transform.position)).sqrMagnitude;
                    if (sqr < closestSqr) { closestSqr = sqr; closest = point; }
                }
                if (!closest)
                {
                    Debug.LogError($"ERROR: TeleportAction - ID가 '{destinationID}'인 지점이 씬에 없습니다.");
                    return;
                }
                target = closest.transform.position;
                physics.ZVelocity = 0f;
            }
            else if (!string.IsNullOrEmpty(destinationID))
            {
                // TargetEntityManipulateAction 안에서는 stateController가 옮길 대상이라 IEvent가 없다.
                EventTargets targets = GameManager.instance.eventManager.currentTargets;
                if (!targets.targetEntities.TryGetValue(destinationID, out EventControllableEntity destination) || !destination)
                {
                    Debug.LogError($"ERROR: TeleportAction - 목적지 대상 '{destinationID}'가 없습니다. " +
                                   "EventSO.involvedEventTargets와 GameStateEvent의 Manual Targets를 확인하세요.");
                    return;
                }
                target = destination.transform.position;
            }

            if (logical) physics.LogicalTeleport(target);
            else
            {
                Vector3 previous = stateController.transform.position;
                physics.RealTeleport(target);
                // Cinemachine은 추적 대상이 순간이동해도 댐핑으로 따라가 화면이 미끄러진다 — 옛 TeleportEventAction처럼 같이 옮긴다.
                CameraManager camera = GameManager.instance.cameraManager;
                if (camera && camera.currentFollowTarget == stateController.transform)
                    camera.StrictMovement(target, previous);
            }
        }
    }
}
