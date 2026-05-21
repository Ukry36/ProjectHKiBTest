using UnityEngine;

[CreateAssetMenu(
    fileName = "SetDirReverseAccuracyFromTargetAction",
    menuName = "State Machine/Action/SetDir/SetDirReverseAccuracyFromTargetAction"
)]
public class SetDirReverseAccuracyFromTargetAction : StateActionSO
{
    [SerializeField] private bool negative;

    public override void Act(StateController stateController)
    {
        if (!stateController.TryGetInterface(out IDirAnimatable animatable)) return;
        if (!stateController.TryGetInterface(out ITargetable targetable)) return;
        if (!stateController.TryGetInterface(out IAttackable attackable)) return;
        if (attackable is not AttackableModule attackableModule) return;
        if (!targetable.CurrentTarget) return;

        Vector2 baseDir = targetable.CurrentTarget.position - stateController.transform.position;
        if (baseDir.sqrMagnitude <= 0.0001f) return;

        baseDir.Normalize();

        bool shouldReverse = attackableModule.TryRollAccuracyDebuff();
        Vector2 finalDir = shouldReverse ? baseDir * -1f : baseDir;

        if (negative)
            finalDir *= -1f;

        if (!animatable.CheckIfLastSetDirectionSame(finalDir))
            animatable.SetAnimationDirection(finalDir);
/*
#if UNITY_EDITOR
        Debug.Log(
            $"[AccuracyReverse/Target] {stateController.name} | " +
            $"chance={attackableModule.AccuracyMissChance:F2} | " +
            $"reversed={shouldReverse} | " +
            $"baseDir={baseDir} | finalDir={finalDir}"
        );
#endif
*/
    }
}