using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;

    public string CurrentAnimation { get; private set; }
    private EnumManager.AnimDir _animationDirection;
    public EnumManager.AnimDir AnimationDirection
    {
        get => _animationDirection;
        private set
        {
            if (_animationDirection != value)
            {
                _animationDirection = value;
                OnDirChanged?.Invoke(value);
            }
        }
    }

    private Vector2 _lastSetAnimationDir;
    public Vector2 LastSetAnimationDir8
    {
        get => _lastSetAnimationDir;
    }

    public Vector2 LastSetAnimationDir4
    {
        get
        {
            if (LastSetAnimationDir8 == Vector2.zero) return Vector2.zero;
            return AnimationDirection switch
            {
                EnumManager.AnimDir.D => Vector2.down,
                EnumManager.AnimDir.L => Vector2.left,
                EnumManager.AnimDir.R => Vector2.right,
                EnumManager.AnimDir.U => Vector2.up,
                _ => Vector2.zero,
            };
        }
    }

    public Quaternion LastSetAnimationQuaternion4
    {
        get
        {
            if (LastSetAnimationDir8 == Vector2.zero) return Quaternion.identity;
            return AnimationDirection switch
            {
                EnumManager.AnimDir.D => Quaternion.identity,
                EnumManager.AnimDir.L => Quaternion.Euler(0, 0, -90),
                EnumManager.AnimDir.R => Quaternion.Euler(0, 0, 90),
                EnumManager.AnimDir.U => Quaternion.Euler(0, 0, 180),
                _ => Quaternion.identity,
            };
        }
    }

    public float LastSetAnimationAngle4
    {
        get
        {
            if (LastSetAnimationDir8 == Vector2.zero) return 0;
            return AnimationDirection switch
            {
                EnumManager.AnimDir.D => 0,
                EnumManager.AnimDir.L => -90,
                EnumManager.AnimDir.R => 90,
                EnumManager.AnimDir.U => 180,
                _ => 0,
            };
        }
    }

    public delegate void OnDirChangedEventHandler(EnumManager.AnimDir animDir);
    public event OnDirChangedEventHandler OnDirChanged;
    public MathManagerSO mathManager;

    public Vector2 GetAnimationRestrictedDirection(Vector3 input)
    {
        if (input.Equals(Vector3.zero)) return Vector2.zero;
        switch (AnimationDirection)
        {
            case EnumManager.AnimDir.D:
                if (input.y > 0) return Vector2.zero;
                if (input.x < -MathManagerSO.tan225) return Vector2.down + Vector2.left;
                if (input.x > MathManagerSO.tan225) return Vector2.down + Vector2.right;
                return input;

            case EnumManager.AnimDir.L:
                if (input.x > 0) return Vector2.zero;
                if (input.y < -MathManagerSO.tan225) return Vector2.left + Vector2.down;
                if (input.y > MathManagerSO.tan225) return Vector2.left + Vector2.up;
                return input;

            case EnumManager.AnimDir.R:
                if (input.x < 0) return Vector2.zero;
                if (input.y < -MathManagerSO.tan225) return Vector2.right + Vector2.down;
                if (input.y > MathManagerSO.tan225) return Vector2.right + Vector2.up;
                return input;

            case EnumManager.AnimDir.U:
                if (input.y < 0) return Vector2.zero;
                if (input.x < -MathManagerSO.tan225) return Vector2.up + Vector2.left;
                if (input.x > MathManagerSO.tan225) return Vector2.up + Vector2.right;
                return input;

            default: return Vector2.zero;
        }
    }

    public bool CheckIfLastSetDirectionSame(Vector2 input)
    => mathManager.SetDirection8One(input).Equals(_lastSetAnimationDir);

    public void SetAnimationDirection(Vector2 vectorDir, bool maintainProgress = false)
    {
        if (vectorDir.Equals(Vector2.zero)) return;
        vectorDir = vectorDir.normalized;
        if (mathManager.Absolute(vectorDir.y) > mathManager.Absolute(vectorDir.x))
            SetAnimationDirectionInternal(vectorDir.y > 0 ? EnumManager.AnimDir.U : EnumManager.AnimDir.D, maintainProgress);
        else
            SetAnimationDirectionInternal(vectorDir.x > 0 ? EnumManager.AnimDir.R : EnumManager.AnimDir.L, maintainProgress);
        _lastSetAnimationDir = mathManager.SetDirection8One(vectorDir);
    }

    public void SetAnimationDirection(EnumManager.AnimDir animDir, bool maintainProgress = false)
    {
        SetAnimationDirectionInternal(animDir, maintainProgress);
        _lastSetAnimationDir = AnimationDirection switch
        {
            EnumManager.AnimDir.D => Vector2.down,
            EnumManager.AnimDir.L => Vector2.left,
            EnumManager.AnimDir.R => Vector2.right,
            EnumManager.AnimDir.U => Vector2.up,
            _ => Vector2.zero,
        };
    }

    private void SetAnimationDirectionInternal(EnumManager.AnimDir animDir, bool maintainProgress)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(CurrentAnimation + "_" + animDir))
            return;
        if (maintainProgress)
        {
            float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            AnimationDirection = animDir;
            animator.Play(CurrentAnimation + "_" + animDir, 0, time + 0.01f);
            return;
        }
        AnimationDirection = animDir;
        animator.Play(CurrentAnimation + "_" + animDir);
    }

    public void Play(string animationName, bool directionDependent)
    {
        CurrentAnimation = animationName;
        animator.Play(animationName + (directionDependent ? "_" + AnimationDirection : ""), 0, 0);
    }
}