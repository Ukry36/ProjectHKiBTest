
public interface ISkinableBase
{
    public SkinDataSO SkinData { get; set; }
}

public interface ISkinable : ISkinableBase, IInitializable
{
    public void ApplySkin(SimpleAnimationDataSO animationData);
}
