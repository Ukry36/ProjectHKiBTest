using UnityEditor.Animations;

public interface ISkinable : ISkinableBase, IInitializable
{
    public void ApplySkin(SimpleAnimationDataSO animationData);
}
