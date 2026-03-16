using UnityEngine;

public interface IAnimatableBase
{
    public SimpleAnimationDataSO AnimationData { get; set; }
}

public interface IAnimatable : IAnimatableBase, IInitializable
{
    public SimpleAnimationPlayer AnimationPlayer { get; set; }
    public void Play(string animationName);
}