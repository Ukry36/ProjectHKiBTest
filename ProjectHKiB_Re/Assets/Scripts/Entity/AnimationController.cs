using UnityEditor.Animations;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    [field: SerializeField] public string CurrentAnimation { get; private set; }

    public virtual void Initialize(AnimatorController animatorController)
    {
        animator.runtimeAnimatorController = animatorController;
        if (CurrentAnimation == "") CurrentAnimation = "Idle";
        Play(CurrentAnimation);
    }

    public void Play(string animationName)
    {
        CurrentAnimation = animationName;
        animator.Play(animationName, 0, 0);
    }
}