using UnityEngine;

public interface IAnimatable : IAnimatableBase, IInitializable
{
    public SimpleAnimationPlayer AnimationPlayer { get; set; }
    public void Play(string animationName);
}