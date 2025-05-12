using UnityEngine;
[CreateAssetMenu(fileName = "AttackMoveGeneralAction", menuName = "Scriptable Objects/State Machine/Action/AttackMoveGeneral", order = 3)]
public class AttackMoveGeneralAction : StateActionSO
{
    [SerializeField] private MovementManagerSO movementManager;
    [SerializeField] private TargetingManagerSO targetingManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) && stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IEntityStateController controller))
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
            int moveRadius = attackData.attackMoveMaxRange;
            Transform thisTransform = stateController.transform;
            AnimationController AC = controller.AnimationController;
            Transform target;
            if (!attackable.CurrentTarget)
            {
                target = targetingManager.PositianalTarget(thisTransform.position, moveRadius, attackable.TargetLayers);
                attackable.CurrentTarget = target;
                if (!target) return;
            }
            Debug.DrawLine(thisTransform.position, attackable.CurrentTarget.position, Color.blue, 0.4f);
            AC.SetAnimationDirection(attackable.CurrentTarget.position - thisTransform.position);
            movementManager.AttackMove(thisTransform, movable, attackable.CurrentTarget.position, moveRadius);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
