using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "Skin Data", menuName = "Scriptable Objects/Data/Skin Data")]
public class SkinDataSO : ScriptableObject
{
    public BodytypeDataSO bodyType;
    public Texture2D skinTexture;
    public Texture2D emissionSkinTexture;
    public Texture2D effectSkinTexture;
}
