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

    public delegate void OnDirChangedEventHandler(EnumManager.AnimDir animDir);
    public event OnDirChangedEventHandler OnDirChanged;
    public MathManagerSO mathManager;

    public void Start()
    {
        OnDirChanged += OnDirChange;
    }

    public void SetAnimationDirection(Vector2 vectorDir)
    {
        if (vectorDir.Equals(Vector2.zero)) return;
        if (mathManager.Absolute(vectorDir.y) - 0.1f > mathManager.Absolute(vectorDir.x))
            SetAnimationDirection(vectorDir.y > 0 ? EnumManager.AnimDir.U : EnumManager.AnimDir.D);
        else
            SetAnimationDirection(vectorDir.x > 0 ? EnumManager.AnimDir.R : EnumManager.AnimDir.L);
    }

    public void SetAnimationDirection(EnumManager.AnimDir animDir) => AnimationDirection = animDir;

    public void Play(string animationName, bool directionDependent)
    {
        CurrentAnimation = animationName;
        animator.Play(animationName + (directionDependent ? "_" + AnimationDirection : ""));
    }

    public void OnDirChange(EnumManager.AnimDir animDir)
    => animator.Play(CurrentAnimation + "_" + animDir);
}