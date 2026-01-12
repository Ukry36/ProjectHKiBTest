using UnityEditor.Animations;
using UnityEngine;

public class AnimatableModule : InterfaceModule, IAnimatable
{
    public SimpleAnimationDataSO AnimationData { get; set; }
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {

        interfaceRegistable.RegisterInterface<IAnimatable>(this);
    }

    [field: SerializeField] public SimpleAnimationPlayer AnimationPlayer { get; set; }
    [field: SerializeField] public string CurrentAnimation { get; protected set; }

    public virtual void Initialize()
    {
        AnimationPlayer.animationData = AnimationData;
        if (CurrentAnimation == "") CurrentAnimation = "Idle";
        Play(CurrentAnimation);
    }

    public void Play(string animationName)
    {
        CurrentAnimation = animationName;
        AnimationPlayer.gameObject.SetActive(true);
        AnimationPlayer.Play(animationName);
    }
}