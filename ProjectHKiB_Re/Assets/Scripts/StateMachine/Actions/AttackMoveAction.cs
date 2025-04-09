using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "AttackMoveAction", menuName = "Scriptable Objects/State Machine/Action/AttackMove", order = 3)]
public class AttackMoveAction : StateActionSO
{
    public string moveTrigger = "attackMove";
    public MovementManagerSO movementManager;
    public TargetingManagerSO targetingManager;
    public override void Act(StateController stateController)
    {
        if (stateController.GetBoolParameter(moveTrigger))
        {
            stateController.SetBoolParameterFalse(moveTrigger);

            if (stateController.TryGetInterFace(out IAttackable attackable) && stateController.TryGetInterFace(out IMovable movable))
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
                AnimationController AC = stateController.animationController;
                Transform target;

                if (!GameManager.instance.inputManager.MoveInput.Equals(Vector2.zero))
                {
                    moveRadius = attackData.attackMoveMaxRange;

                    if (autoTarget)
                    {
                        target = targetingManager.DirectionalTarget(attackable.CurrentTarget, thisTransform.position, moveRadius, AC.GetAnimationRestrictedPlayerInputDirection(), attackable.TargetLayers);
                        if (target)
                        {
                            attackable.CurrentTarget = target;
                            movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                            return;
                        }
                        attackable.CurrentTarget = null;
                    }

                    movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)AC.GetAnimationRestrictedPlayerInputDirection() * moveRadius, moveRadius);
                }
                else
                {
                    moveRadius = attackData.attackMoveMaxRange;

                    if (autoTarget && attackable.CurrentTarget && targetingManager.CheckCurrentTargetDistance(attackable.CurrentTarget, thisTransform.position, moveRadius))
                    {
                        Debug.DrawLine(thisTransform.position, attackable.CurrentTarget.position, Color.blue, 0.4f);
                        AC.SetAnimationDirection(attackable.CurrentTarget.position - thisTransform.position, true);
                        movementManager.AttackMove(thisTransform, movable, attackable.CurrentTarget.position, moveRadius);
                        return;
                    }

                    target = targetingManager.PositianalTarget(thisTransform.position, moveRadius, attackable.TargetLayers);
                    if (target)
                    {
                        AC.SetAnimationDirection(target.position - thisTransform.position, true);
                        movementManager.AttackMove(thisTransform, movable, target.position, moveRadius);
                        attackable.CurrentTarget = target;
                        return;
                    }
                    moveRadius = attackData.attackMoveMinRange;
                    movementManager.AttackMove(thisTransform, movable, thisTransform.position + (Vector3)AC.GetAnimationDirection() * moveRadius, moveRadius);
                }
                movable.FootstepController.PlayFootstepAudio(default);
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}
