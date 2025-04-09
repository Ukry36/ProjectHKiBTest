using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    public Damager damager;

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

    public delegate void OnDirChangedEventHandler(EnumManager.AnimDir animDir);
    public event OnDirChangedEventHandler OnDirChanged;
    public MathManagerSO mathManager;

    public void Start()
    {
        if (damager)
            OnDirChanged += damager.SetDamageDirection;
    }

    public Vector2 GetAnimationDirection() => AnimationDirection switch
    {
        EnumManager.AnimDir.D => Vector2.down,
        EnumManager.AnimDir.L => Vector2.left,
        EnumManager.AnimDir.R => Vector2.right,
        EnumManager.AnimDir.U => Vector2.up,
        _ => Vector2.zero,
    };

    public Vector2 GetAnimationRestrictedPlayerInputDirection()
    {
        Vector3 input = GameManager.instance.inputManager.MoveInput;
        if (input.Equals(Vector3.zero)) return Vector2.zero;
        switch (AnimationDirection)
        {
            case EnumManager.AnimDir.D:
                if (input.y > 0) return Vector2.zero;
                if (input.x < -MathManagerSO.sqrt2) return Vector2.down + Vector2.left;
                if (input.x > MathManagerSO.sqrt2) return Vector2.down + Vector2.right;
                return input;

            case EnumManager.AnimDir.L:
                if (input.x > 0) return Vector2.zero;
                if (input.y < -MathManagerSO.sqrt2) return Vector2.left + Vector2.down;
                if (input.y > MathManagerSO.sqrt2) return Vector2.left + Vector2.up;
                return input;

            case EnumManager.AnimDir.R:
                if (input.x < 0) return Vector2.zero;
                if (input.y < -MathManagerSO.sqrt2) return Vector2.right + Vector2.down;
                if (input.y > MathManagerSO.sqrt2) return Vector2.right + Vector2.up;
                return input;

            case EnumManager.AnimDir.U:
                if (input.y < 0) return Vector2.zero;
                if (input.x < -MathManagerSO.sqrt2) return Vector2.up + Vector2.left;
                if (input.x > MathManagerSO.sqrt2) return Vector2.up + Vector2.right;
                return input;

            default: return Vector2.zero;
        }
    }

    public void SetAnimationDirection(Vector2 vectorDir, bool maintainProgress = false)
    {
        if (vectorDir.Equals(Vector2.zero)) return;
        if (mathManager.Absolute(vectorDir.y) - 0.1f > mathManager.Absolute(vectorDir.x))
            SetAnimationDirection(vectorDir.y > 0 ? EnumManager.AnimDir.U : EnumManager.AnimDir.D, maintainProgress);
        else
            SetAnimationDirection(vectorDir.x > 0 ? EnumManager.AnimDir.R : EnumManager.AnimDir.L, maintainProgress);
    }

    public void SetAnimationDirection(EnumManager.AnimDir animDir, bool maintainProgress = false)
    {
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
        animator.Play(animationName + (directionDependent ? "_" + AnimationDirection : ""));
    }
}