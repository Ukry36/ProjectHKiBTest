using UnityEngine;

public interface IAnimatable : IAnimatableBase, IInitializable
{
    public Animator Animator { get; set; }
    public void Play(string animationName);
}