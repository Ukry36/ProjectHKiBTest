using UnityEditor.Animations;

public interface IDirAnimatable
{
    public AnimatorController AnimatorController { get; set; }
    public DirAnimationController AnimationController { get; set; }
}