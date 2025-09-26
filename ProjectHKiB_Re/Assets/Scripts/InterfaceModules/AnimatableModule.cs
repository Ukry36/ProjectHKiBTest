using UnityEditor.Animations;
using UnityEngine;

public class AnimatableModule : InterfaceModule, IAnimatable
{
    public AnimatorController AnimatorController { get; set; }
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {

        interfaceRegistable.RegisterInterface<IAnimatable>(this);
    }

    [field: SerializeField] public Animator Animator { get; set; }
    [field: SerializeField] public string CurrentAnimation { get; protected set; }

    public virtual void Initialize()
    {
        Animator.runtimeAnimatorController = AnimatorController;
        if (CurrentAnimation == "") CurrentAnimation = "Idle";
        Play(CurrentAnimation);
    }

    public void Play(string animationName)
    {
        CurrentAnimation = animationName;
        Animator.gameObject.SetActive(true);
        Animator.Play(animationName, 0, 0);
    }
}