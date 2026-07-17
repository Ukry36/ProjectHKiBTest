using UnityEngine;
using UnityEngine.U2D.Animation;
public interface ISkinableBase
{
    public SkinDataSO SkinData { get; set; }
}

public interface ISkinable : ISkinableBase, IInitializable
{
    public void ApplySkin(SimpleAnimationDataSO animationData);
}

public class SkinableModule : InterfaceModule, ISkinable
{
    [field: SerializeField] public SkinDataSO SkinData { get; set; }
    [SerializeField] protected SpriteLibrary mainSpriteLibrary;
    [SerializeField] protected SpriteRenderer mainSpriteRenderer;
    [SerializeField] protected SpriteRenderer effectSpriteRenderer;

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<ISkinable>(this);
    }

    public virtual void Initialize()
    {

    }

    public virtual void ApplySkin(SimpleAnimationDataSO animationData)
    {
        if (animationData == null) return;
        if (SkinData == null) return;
        mainSpriteLibrary.spriteLibraryAsset = SkinData.bodyType.Bodytypes[animationData];
        MaterialPropertyBlock materialPropertyBlock = new();
        materialPropertyBlock.SetTexture("_MainTex", SkinData.bodyType.MainTex[animationData]);
        materialPropertyBlock.SetTexture("_SkinTex", SkinData.skinTexture);
        materialPropertyBlock.SetTexture("_EmissionSkinTex", SkinData.emissionSkinTexture);
        mainSpriteRenderer.SetPropertyBlock(materialPropertyBlock);

        materialPropertyBlock.SetTexture("_SkinTex", SkinData.effectSkinTexture);
        effectSpriteRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
