using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;
namespace Assets.Scripts.Interfaces.Modules
{
    public class SkinableModule : InterfaceModule, ISkinable
    {
        [field: SerializeField] public int Amount { get; set; }
        [field: SerializeField] public SkinDataSO SkinData { get; set; }
        public SpriteLibrary spriteLibrary;
        public SpriteRenderer spriteRenderer;

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<ISkinable>(this);
        }
        public void ApplySkin(AnimatorController animatorController)
        {
            spriteLibrary.spriteLibraryAsset = SkinData.bodyType.Bodytypes[animatorController];
            MaterialPropertyBlock materialPropertyBlock = new();
            materialPropertyBlock.SetTexture("_MainTex", SkinData.bodyType.MainTex[animatorController]);
            materialPropertyBlock.SetTexture("_SkinTex", SkinData.skinTexture);
            materialPropertyBlock.SetTexture("_EmissionSkinTex", SkinData.emissionSkinTexture);
            spriteRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }
}