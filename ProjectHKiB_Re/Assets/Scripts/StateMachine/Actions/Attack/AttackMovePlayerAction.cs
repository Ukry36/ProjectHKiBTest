using UnityEngine;
[CreateAssetMenu(fileName = "AttackMovePlayerAction", menuName = "State Machine/Action/Attack/AttackMovePlayer")]
public class AttackMovePlayerAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public TargetingManagerSO targetingManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable)
        && stateController.TryGetInterface(out IMovable movable)
        && stateController.TryGetInterface(out IDirAnimatable animatable)
        && stateController.TryGetInterface(out ITargetable targetable)
        && stateController.TryGetInterface(out IFootstep footstep))
        {
            if (attackable.AttackDatas.Equals(null))
            {
                Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
            }
            if (attackable.AttackDatas.Length - 1 < attackable.AttackNumber)
            {
                Debug.LogError("ERROR: AttackData[" + attackable.AttackNumber + "] is missing !!!"); return;
            }

            AttackDataSO attackData = attackable.AttackDatas[attackable.AttackNumber];
            int moveRadius;
            bool autoTarget = attackData.isAutoTarget;
            Transform thisTransform = stateController.transform;
            Transform target;

            if (!GameManager.instance.inputManager.MoveInput.Equals(Vector2.zero))
            {
                moveRadius = attackData.attackMoveMaxRange;

                if (autoTarget)
                {
                    target = targetingManager.DirectionalTarget(targetable.CurrentTarget, thisTransform.position, moveRadius, animatable.GetAnimationRestrictedDirection(GameManager.instance.inputManager.MoveInput), targetable.TargetLayers);
                    if (target)
                    {
                        targetable.CurrentTarget = target;
                        movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                        return;
                    }
                    targetable.CurrentTarget = null;
                }

                movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)animatable.GetAnimationRestrictedDirection(GameManager.instance.inputManager.MoveInput) * moveRadius, moveRadius);
            }
            else
            {
                moveRadius = attackData.attackMoveMaxRange;

                if (autoTarget && targetable.CurrentTarget && targetingManager.CheckCurrentTargetDistance(targetable.CurrentTarget, thisTransform.position, moveRadius))
                {
                    Debug.DrawLine(thisTransform.position, targetable.CurrentTarget.position, Color.blue, 0.4f);
                    animatable.SetAnimationDirection(targetable.CurrentTarget.position - thisTransform.position);
                    movementManager.AttackMove(thisTransform, movable, targetable.CurrentTarget.position, moveRadius);
                    return;
                }

                target = targetingManager.PositianalTarget(thisTransform.position, moveRadius, targetable.TargetLayers);
                if (target)
                {
                    animatable.SetAnimationDirection(target.position - thisTransform.position);
                    movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                    targetable.CurrentTarget = target;
                    return;
                }
                targetable.CurrentTarget = null;
                moveRadius = attackData.attackMoveMinRange;
                movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)animatable.LastSetAnimationDir4 * moveRadius, moveRadius);
            }
            footstep.PlayFootstepAudio(default);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
