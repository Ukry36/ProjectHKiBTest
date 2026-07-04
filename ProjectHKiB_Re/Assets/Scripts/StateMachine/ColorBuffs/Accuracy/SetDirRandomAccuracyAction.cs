using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class SetDirRandomAccuracyAction : StateAction
    {
        [SerializeField] private bool negative;

        [Header("Accuracy Debuff Random Offset")]
        [SerializeField, Range(0f, 180f)] private float minAngleOffset = 30f;
        [SerializeField, Range(0f, 180f)] private float maxAngleOffset = 75f;

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

            bool shouldDistort = attackableModule.TryRollAccuracyDebuff();
            Vector2 finalDir = baseDir;

            if (shouldDistort)
            {
                float angle = Random.Range(minAngleOffset, maxAngleOffset);
                float sign = Random.value < 0.5f ? -1f : 1f;
                finalDir = Rotate(baseDir, angle * sign).normalized;
            }

            if (negative)
                finalDir *= -1f;

            if (!animatable.CheckIfLastSetDirectionSame(finalDir))
                animatable.SetAnimationDirection(finalDir);
            /*
            #if UNITY_EDITOR
                    Debug.Log(
                        $"[AccuracyDir] {stateController.name} | chance={attackableModule.AccuracyMissChance:F2} | " +
                        $"distorted={shouldDistort} | dir={finalDir}"
                    );
            #endif
            */
        }

        private Vector2 Rotate(Vector2 dir, float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                dir.x * cos - dir.y * sin,
                dir.x * sin + dir.y * cos
            );
        }
    }
}