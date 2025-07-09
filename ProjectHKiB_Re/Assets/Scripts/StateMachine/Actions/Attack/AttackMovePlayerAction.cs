using UnityEngine;
[CreateAssetMenu(fileName = "AttackMovePlayerAction", menuName = "Scriptable Objects/State Machine/Action/Attack/AttackMovePlayer")]
public class AttackMovePlayerAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public TargetingManagerSO targetingManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable)
        && stateController.TryGetInterface(out IMovable movable)
        && stateController.TryGetInterface(out IDirAnimatable animatable)
        && stateController.TryGetInterface(out ITargetable targetable))
        {
            if (attackable.AttackDatas.Equals(null))
            {
                Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
            }
            if (attackable.AttackDatas.Length - 1 < attackable.AttackController.AttackNumber)
            {
                Debug.LogError("ERROR: AttackData[" + attackable.AttackController.AttackNumber + "] is missing !!!"); return;
            }

            AttackDataSO attackData = attackable.AttackDatas[attackable.AttackController.AttackNumber];
            int moveRadius;
            bool autoTarget = attackData.isAutoTarget;
            Transform thisTransform = stateController.transform;
            DirAnimationController AC = animatable.AnimationController;
            Transform target;

            if (!GameManager.instance.inputManager.MoveInput.Equals(Vector2.zero))
            {
                moveRadius = attackData.attackMoveMaxRange;

                if (autoTarget)
                {
                    target = targetingManager.DirectionalTarget(targetable.CurrentTarget, thisTransform.position, moveRadius, AC.GetAnimationRestrictedDirection(GameManager.instance.inputManager.MoveInput), targetable.TargetLayers);
                    if (target)
                    {
                        targetable.CurrentTarget = target;
                        movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                        return;
                    }
                    targetable.CurrentTarget = null;
                }

                movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)AC.GetAnimationRestrictedDirection(GameManager.instance.inputManager.MoveInput) * moveRadius, moveRadius);
            }
            else
            {
                moveRadius = attackData.attackMoveMaxRange;

                if (autoTarget && targetable.CurrentTarget && targetingManager.CheckCurrentTargetDistance(targetable.CurrentTarget, thisTransform.position, moveRadius))
                {
                    Debug.DrawLine(thisTransform.position, targetable.CurrentTarget.position, Color.blue, 0.4f);
                    AC.SetAnimationDirection(targetable.CurrentTarget.position - thisTransform.position);
                    movementManager.AttackMove(thisTransform, movable, targetable.CurrentTarget.position, moveRadius);
                    return;
                }

                target = targetingManager.PositianalTarget(thisTransform.position, moveRadius, targetable.TargetLayers);
                if (target)
                {
                    AC.SetAnimationDirection(target.position - thisTransform.position);
                    movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                    targetable.CurrentTarget = target;
                    return;
                }
                targetable.CurrentTarget = null;
                moveRadius = attackData.attackMoveMinRange;
                movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)AC.LastSetAnimationDir4 * moveRadius, moveRadius);
            }
            movable.FootstepController.PlayFootstepAudio(default);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
