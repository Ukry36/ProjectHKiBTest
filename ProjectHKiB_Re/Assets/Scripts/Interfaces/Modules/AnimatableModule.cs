using UnityEditor.Animations;
using UnityEngine;

public class AnimatableModule : InterfaceModule, IAnimatable
{
    [field: SerializeField] public AnimatorController AnimatorController { get; set; }
    [field: SerializeField] public AnimationController AnimationController { get; set; }
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        AnimationController.animator.runtimeAnimatorController = AnimatorController;
        interfaceRegistable.RegisterInterface<IAnimatable>(this);
    }
}