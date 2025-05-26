using UnityEditor.Animations;

public interface IAnimatable
{
    public AnimatorController AnimatorController { get; set; }
    public AnimationController AnimationController { get; set; }
}