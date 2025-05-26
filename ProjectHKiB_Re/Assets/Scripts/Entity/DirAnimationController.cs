using UnityEditor.Animations;
using UnityEngine;

public class DirAnimationController : AnimationController
{
    public MathManagerSO mathManager;
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

    public Vector2 LastSetAnimationDir8
    {
        get => _lastSetAnimationDir8;
    }

    public Vector2 LastSetAnimationDir4
    {
        get => _lastSetAnimationDir4;
    }

    public Quaternion LastSetAnimationQuaternion4
    {
        get
        {
            if (_lastSetAnimationDir8 == Vector2.zero) return Quaternion.identity;
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
            if (_lastSetAnimationDir8 == Vector2.zero) return 0;
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

    public override void Initialize(AnimatorController animatorController)
    {
        base.Initialize(animatorController);
        if (_lastSetAnimationDir4 == Vector2.zero) _lastSetAnimationDir4 = Vector2.down;
        if (_lastSetAnimationDir8 == Vector2.zero) _lastSetAnimationDir8 = Vector2.down;
    }

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
    /*
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
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(CurrentAnimation + animDir))
                return;
            AnimationDirection = animDir;
            if (maintainProgress)
            {
                float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                animator.Play(CurrentAnimation + animDir, 0, time + 0.01f);
                return;
            }
            animator.Play(CurrentAnimation + animDir);
        }
    */
    private Vector2 _lastSetAnimationDir8;
    private Vector2 _lastSetAnimationDir4;

    public bool CheckIfLastSetDirectionSame(Vector2 input)
   => mathManager.SetDirection8One(input).Equals(_lastSetAnimationDir8);

    public void SetAnimationDirection(Vector2 vectorDir)
    {
        if (vectorDir == Vector2.zero) return;
        vectorDir = mathManager.SetDirection8One(vectorDir);
        if (vectorDir == _lastSetAnimationDir8) return;

        _lastSetAnimationDir8 = vectorDir;
        vectorDir.y = (vectorDir.x != 0 && vectorDir.y != 0) ? 0 : vectorDir.y;
        _lastSetAnimationDir4 = vectorDir;
        animator.SetFloat("dirX", vectorDir.x);
        animator.SetFloat("dirY", vectorDir.y);

        AnimationDirection = _lastSetAnimationDir4.y < 0 ? EnumManager.AnimDir.D :
                             _lastSetAnimationDir4.x < 0 ? EnumManager.AnimDir.L :
                             _lastSetAnimationDir4.x > 0 ? EnumManager.AnimDir.R :
                             _lastSetAnimationDir4.y > 0 ? EnumManager.AnimDir.U :
                             AnimationDirection;
    }

    public void SetAnimationDirection(EnumManager.AnimDir animDir)
    {
        Vector2 dir = animDir switch
        {
            EnumManager.AnimDir.D => Vector2.down,
            EnumManager.AnimDir.L => Vector2.left,
            EnumManager.AnimDir.R => Vector2.right,
            EnumManager.AnimDir.U => Vector2.up,
            _ => Vector2.zero,
        };

        _lastSetAnimationDir4 = dir;
        _lastSetAnimationDir8 = dir;
        if (dir == Vector2.zero) return;
        animator.SetFloat("dirX", dir.x);
        animator.SetFloat("dirY", dir.y);
        AnimationDirection = animDir;
    }
}