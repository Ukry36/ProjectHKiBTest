using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "Skin Data", menuName = "Scriptable Objects/Data/Skin Data", order = 1)]
public class SkinDataSO : ScriptableObject
{
    public BodytypeDataSO bodyType;
    public Texture2D skinTexture;
    public Texture2D emissionSkinTexture;

    public void SetSKin(SpriteLibrary spriteLibrary, AnimatorController animatorController, SpriteRenderer spriteRenderer)
    {
        spriteLibrary.spriteLibraryAsset = bodyType.Bodytypes[animatorController];
        MaterialPropertyBlock materialPropertyBlock = new();
        materialPropertyBlock.SetTexture("_SkinTex", skinTexture);
        materialPropertyBlock.SetTexture("_EmissionSkinTex", emissionSkinTexture);
        spriteRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
