using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "BodytypeDat", menuName = "Scriptable Objects/Data/BodytypeDat", order = 1)]
public class BodytypeDataSO : ScriptableObject
{
    public SerializedDictionary<AnimatorController, SpriteLibraryAsset> Bodytypes;
    public SerializedDictionary<AnimatorController, Texture2D> MainTex;
}
