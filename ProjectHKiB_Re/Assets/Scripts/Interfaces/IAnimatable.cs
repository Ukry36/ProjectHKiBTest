using UnityEngine;

public interface IAnimatable : IAnimatableBase
{
    public Animator Animator { get; set; }
    public void Play(string animationName);
    public void Initialize();
}