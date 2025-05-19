using UnityEngine;
[CreateAssetMenu(fileName = "AttackMovePlayerAction", menuName = "Scriptable Objects/State Machine/Action/AttackMovePlayer", order = 3)]
public class AttackMovePlayerAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public TargetingManagerSO targetingManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) && stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDirAnimatable animatable))
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
                    target = targetingManager.DirectionalTarget(attackable.CurrentTarget, thisTransform.position, moveRadius, AC.GetAnimationRestrictedDirection(GameManager.instance.inputManager.MoveInput), attackable.TargetLayers);
                    if (target)
                    {
                        attackable.CurrentTarget = target;
                        movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                        return;
                    }
                    attackable.CurrentTarget = null;
                }

                movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)AC.GetAnimationRestrictedDirection(GameManager.instance.inputManager.MoveInput) * moveRadius, moveRadius);
            }
            else
            {
                moveRadius = attackData.attackMoveMaxRange;

                if (autoTarget && attackable.CurrentTarget && targetingManager.CheckCurrentTargetDistance(attackable.CurrentTarget, thisTransform.position, moveRadius))
                {
                    Debug.DrawLine(thisTransform.position, attackable.CurrentTarget.position, Color.blue, 0.4f);
                    AC.SetAnimationDirection(attackable.CurrentTarget.position - thisTransform.position);
                    movementManager.AttackMove(thisTransform, movable, attackable.CurrentTarget.position, moveRadius);
                    return;
                }

                target = targetingManager.PositianalTarget(thisTransform.position, moveRadius, attackable.TargetLayers);
                if (target)
                {
                    AC.SetAnimationDirection(target.position - thisTransform.position);
                    movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                    attackable.CurrentTarget = target;
                    return;
                }
                attackable.CurrentTarget = null;
                moveRadius = attackData.attackMoveMinRange;
                movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)AC.LastSetAnimationDir4 * moveRadius, moveRadius);
            }
            movable.FootstepController.PlayFootstepAudio(default);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
