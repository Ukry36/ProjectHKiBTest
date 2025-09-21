using UnityEditor.Animations;

public interface ISkinable : ISkinableBase
{
    public void ApplySkin(AnimatorController animatorController);
}
